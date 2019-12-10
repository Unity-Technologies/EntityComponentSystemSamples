using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using UnityEngine.Assertions;

// Stores the impulse to be applied by the character controller body
public struct DeferredCharacterControllerImpulse
{
    public Entity Entity;
    public float3 Impulse;
    public float3 Point;
}

public static class CharacterControllerUtilities
{
    const float k_SimplexSolverEpsilon = 0.0001f;
    const float k_SimplexSolverEpsilonSq = k_SimplexSolverEpsilon * k_SimplexSolverEpsilon;

    const int k_DefaultQueryHitsCapacity = 8;
    const int k_DefaultConstraintsCapacity = 2 * k_DefaultQueryHitsCapacity;

    public enum CharacterSupportState : byte
    {
        Unsupported = 0,
        Sliding,
        Supported
    }

    public struct CharacterControllerStepInput
    {
        public PhysicsWorld World;
        public float DeltaTime;
        public float3 Gravity;
        public float3 Up;
        public int MaxIterations;
        public float Tau;
        public float Damping;
        public float SkinWidth;
        public float ContactTolerance;
        public float MaxSlope;
        public int RigidBodyIndex;
        public float3 CurrentVelocity;
        public float MaxMovementSpeed;
    }

    // A collector which stores every hit up to the length of the provided native array.
    // To filter out self hits, it stores the rigid body index of the body representing
    // the character controller. Unfortunately, it needs to do this in TransformNewHits
    // since during AddHit rigid body index is not exposed.
    // https://github.com/Unity-Technologies/Unity.Physics/issues/256
    public struct SelfFilteringAllHitsCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        private int m_selfRBIndex;

        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; }
        public int NumHits => AllHits.Length;

        public NativeList<T> AllHits;

        public SelfFilteringAllHitsCollector(int rbIndex, float maxFraction, ref NativeList<T> allHits)
        {
            MaxFraction = maxFraction;
            AllHits = allHits;
            m_selfRBIndex = rbIndex;
        }

        #region IQueryResult implementation

        public bool AddHit(T hit)
        {
            Assert.IsTrue(hit.Fraction < MaxFraction);
            AllHits.Add(hit);
            return true;
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Unity.Physics.Math.MTransform transform, uint numSubKeyBits, uint subKey)
        {
            for (int i = oldNumHits; i < NumHits; i++)
            {
                T hit = AllHits[i];
                hit.Transform(transform, numSubKeyBits, subKey);
                AllHits[i] = hit;
            }
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Unity.Physics.Math.MTransform transform, int rigidBodyIndex)
        {
            if (rigidBodyIndex == m_selfRBIndex)
            {
                for (int i = oldNumHits; i < NumHits; i++)
                {
                    AllHits.RemoveAtSwapBack(oldNumHits);
                }

                return;
            }

            for (int i = oldNumHits; i < NumHits; i++)
            {
                T hit = AllHits[i];
                hit.Transform(transform, rigidBodyIndex);
                AllHits[i] = hit;
            }
        }

        #endregion
    }

    // A collector which stores only the closest hit different from itself.
    public struct SelfFilteringClosestHitCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; private set; }
        public int NumHits { get; private set; }

        private T m_OldHit;
        private T m_ClosestHit;
        public T ClosestHit => m_ClosestHit;

        private int m_selfRBIndex;

        public SelfFilteringClosestHitCollector(int rbIndex, float maxFraction)
        {
            MaxFraction = maxFraction;
            m_OldHit = default(T);
            m_ClosestHit = default(T);
            NumHits = 0;
            m_selfRBIndex = rbIndex;
        }

        #region ICollector

        public bool AddHit(T hit)
        {
            Assert.IsTrue(hit.Fraction <= MaxFraction);
            MaxFraction = hit.Fraction;
            m_OldHit = m_ClosestHit;
            m_ClosestHit = hit;
            NumHits = 1;
            return true;
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Unity.Physics.Math.MTransform transform, uint numSubKeyBits, uint subKey)
        {
            if (m_ClosestHit.Fraction < oldFraction)
            {
                m_ClosestHit.Transform(transform, numSubKeyBits, subKey);
            }
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Unity.Physics.Math.MTransform transform, int rigidBodyIndex)
        {
            if (rigidBodyIndex == m_selfRBIndex)
            {
                m_ClosestHit = m_OldHit;
                NumHits = 0;
                MaxFraction = oldFraction;
                m_OldHit = default(T);
                return;
            }

            if (m_ClosestHit.Fraction < oldFraction)
            {
                m_ClosestHit.Transform(transform, rigidBodyIndex);
            }
        }

        #endregion
    }

    public static unsafe void CheckSupport(
        ref PhysicsWorld world, ref PhysicsCollider collider, CharacterControllerStepInput stepInput, RigidTransform transform, float maxSlope,
        out CharacterSupportState characterState, out float3 surfaceNormal, out float3 surfaceVelocity)
    {
        surfaceNormal = float3.zero;
        surfaceVelocity = float3.zero;

        // Query the world
        NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
        SelfFilteringAllHitsCollector<DistanceHit> distanceHitsCollector = new SelfFilteringAllHitsCollector<DistanceHit>(
            stepInput.RigidBodyIndex, stepInput.ContactTolerance, ref distanceHits);
        {
            ColliderDistanceInput input = new ColliderDistanceInput()
            {
                MaxDistance = stepInput.ContactTolerance,
                Transform = transform,
                Collider = collider.ColliderPtr
            };
            world.CalculateDistance(input, ref distanceHitsCollector);
        }

        // If no hits, proclaim unsupported state
        if (distanceHitsCollector.NumHits == 0)
        {
            characterState = CharacterSupportState.Unsupported;
            return;
        }

        // Downwards direction must be normalized
        float3 downwardsDirection = -stepInput.Up;
        Assert.IsTrue(Unity.Physics.Math.IsNormalized(downwardsDirection));

        float maxSlopeCos = math.cos(maxSlope);

        // Iterate over distance hits and create constraints from them
        NativeList<SurfaceConstraintInfo> constraints = new NativeList<SurfaceConstraintInfo>(k_DefaultConstraintsCapacity, Allocator.Temp);
        for (int i = 0; i < distanceHitsCollector.NumHits; i++)
        {
            DistanceHit hit = distanceHitsCollector.AllHits[i];
            CreateConstraint(stepInput.World, stepInput.Up,
                hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal, hit.Distance,
                stepInput.SkinWidth, maxSlopeCos, ref constraints);
        }

        float3 initialVelocity;
        {
            float velAlongDownwardsDir = math.dot(stepInput.CurrentVelocity, downwardsDirection);
            bool velocityIsAlongDownwardsDirection = velAlongDownwardsDir > 0.0f;
            if (velocityIsAlongDownwardsDirection)
            {
                float3 downwardsVelocity = velAlongDownwardsDir * downwardsDirection;
                initialVelocity =
                    math.select(downwardsVelocity, downwardsDirection, math.abs(velAlongDownwardsDir) > 1.0f) +
                    stepInput.Gravity * stepInput.DeltaTime;
            }
            else
            {
                initialVelocity = downwardsDirection;
            }
        }

        // Solve downwards (don't use min delta time, try to solve full step)
        float3 outVelocity = initialVelocity;
        float3 outPosition = transform.pos;
        SimplexSolver.Solve(stepInput.World, stepInput.DeltaTime, stepInput.DeltaTime, stepInput.Up, stepInput.MaxMovementSpeed,
            constraints, ref outPosition, ref outVelocity, out float integratedTime, false);

        // Get info on surface
        {
            int numSupportingPlanes = 0;
            for (int j = 0; j < constraints.Length; j++)
            {
                var constraint = constraints[j];
                if (constraint.Touched && !constraint.IsTooSteep)
                {
                    numSupportingPlanes++;
                    surfaceNormal += constraint.Plane.Normal;
                    surfaceVelocity += constraint.Velocity;
                }
            }

            if (numSupportingPlanes > 0)
            {
                float invNumSupportingPlanes = 1.0f / numSupportingPlanes;
                surfaceNormal *= invNumSupportingPlanes;
                surfaceVelocity *= invNumSupportingPlanes;

                surfaceNormal = math.normalize(surfaceNormal);
            }
        }

        // Check support state
        {
            if (math.lengthsq(initialVelocity - outVelocity) < k_SimplexSolverEpsilonSq)
            {
                // If velocity hasn't changed significantly, declare unsupported state
                characterState = CharacterSupportState.Unsupported;
            }
            else if (math.lengthsq(outVelocity) < k_SimplexSolverEpsilonSq)
            {
                // If velocity is very small, declare supported state
                characterState = CharacterSupportState.Supported;
            }
            else
            {
                // Check if sliding or supported
                outVelocity = math.normalize(outVelocity);
                float slopeAngleSin = math.max(0.0f, math.dot(outVelocity, downwardsDirection));
                float slopeAngleCosSq = 1 - slopeAngleSin * slopeAngleSin;
                if (slopeAngleCosSq < maxSlopeCos * maxSlopeCos)
                {
                    characterState = CharacterSupportState.Sliding;
                }
                else
                {
                    characterState = CharacterSupportState.Supported;
                }
            }
        }
    }

    public static unsafe void CollideAndIntegrate(
        CharacterControllerStepInput stepInput, float characterMass, bool affectBodies, Collider* collider,
        ref RigidTransform transform, ref float3 linearVelocity, ref NativeStream.Writer deferredImpulseWriter)
    {
        // Copy parameters
        float deltaTime = stepInput.DeltaTime;
        float3 up = stepInput.Up;
        PhysicsWorld world = stepInput.World;

        float remainingTime = deltaTime;

        float3 newPosition = transform.pos;
        quaternion orientation = transform.rot;
        float3 newVelocity = linearVelocity;

        float maxSlopeCos = math.cos(stepInput.MaxSlope);

        const float timeEpsilon = 0.000001f;
        for (int i = 0; i < stepInput.MaxIterations && remainingTime > timeEpsilon; i++)
        {
            NativeList<SurfaceConstraintInfo> constraints = new NativeList<SurfaceConstraintInfo>(k_DefaultConstraintsCapacity, Allocator.Temp);

            // Do a collider cast
            {
                float3 displacement = newVelocity * remainingTime;
                NativeList<ColliderCastHit> castHits = new NativeList<ColliderCastHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
                SelfFilteringAllHitsCollector<ColliderCastHit> collector = new SelfFilteringAllHitsCollector<ColliderCastHit>(stepInput.RigidBodyIndex, 1.0f, ref castHits);
                ColliderCastInput input = new ColliderCastInput()
                {
                    Collider = collider,
                    Orientation = orientation,
                    Start = newPosition,
                    End = newPosition + displacement
                };
                world.CastCollider(input, ref collector);

                // Iterate over hits and create constraints from them
                for (int hitIndex = 0; hitIndex < collector.NumHits; hitIndex++)
                {
                    ColliderCastHit hit = collector.AllHits[hitIndex];
                    CreateConstraint(stepInput.World, stepInput.Up,
                        hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal, hit.Fraction * math.length(displacement),
                        stepInput.SkinWidth, maxSlopeCos, ref constraints);
                }
            }

            // Then do a collider distance for penetration recovery,
            // but only fix up penetrating hits
            {
                // Collider distance query
                NativeList<DistanceHit> distanceHits = new NativeList<DistanceHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
                SelfFilteringAllHitsCollector<DistanceHit> distanceHitsCollector = new SelfFilteringAllHitsCollector<DistanceHit>(
                    stepInput.RigidBodyIndex, stepInput.ContactTolerance, ref distanceHits);
                {
                    ColliderDistanceInput input = new ColliderDistanceInput()
                    {
                        MaxDistance = stepInput.ContactTolerance,
                        Transform = transform,
                        Collider = collider
                    };
                    world.CalculateDistance(input, ref distanceHitsCollector);
                }

                // Iterate over penetrating hits and fix up distance and normal
                int numConstraints = constraints.Length;
                for (int hitIndex = 0; hitIndex < distanceHitsCollector.NumHits; hitIndex++)
                {
                    DistanceHit hit = distanceHitsCollector.AllHits[hitIndex];
                    if (hit.Distance < stepInput.SkinWidth)
                    {
                        bool found = false;

                        // Iterate backwards to locate the original constraint before the max slope constraint
                        for (int constraintIndex = numConstraints - 1; constraintIndex >= 0; constraintIndex--)
                        {
                            SurfaceConstraintInfo constraint = constraints[constraintIndex];
                            if (constraint.RigidBodyIndex == hit.RigidBodyIndex &&
                                constraint.ColliderKey.Equals(hit.ColliderKey))
                            {
                                // Fix up the constraint (normal, distance)
                                {
                                    // Create new constraint
                                    CreateConstraintFromHit(world, hit.RigidBodyIndex, hit.ColliderKey,
                                        hit.Position, hit.SurfaceNormal, hit.Distance,
                                        stepInput.SkinWidth, out SurfaceConstraintInfo newConstraint);

                                    // Resolve its penetration
                                    ResolveConstraintPenetration(ref newConstraint);

                                    // Write back
                                    constraints[constraintIndex] = newConstraint;
                                }

                                found = true;
                                break;
                            }
                        }

                        // Add penetrating hit not caught by collider cast
                        if (!found)
                        {
                            CreateConstraint(stepInput.World, stepInput.Up,
                                hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal, hit.Distance,
                                stepInput.SkinWidth, maxSlopeCos, ref constraints);
                        }
                    }
                }
            }

            // Min delta time for solver to break
            float minDeltaTime = 0.0f;
            if (math.lengthsq(newVelocity) > k_SimplexSolverEpsilonSq)
            {
                // Min delta time to travel at least 1cm
                minDeltaTime = 0.01f / math.length(newVelocity);
            }

            // Solve
            float3 prevVelocity = newVelocity;
            float3 prevPosition = newPosition;
            SimplexSolver.Solve(world, remainingTime, minDeltaTime, up, stepInput.MaxMovementSpeed, constraints, ref newPosition, ref newVelocity, out float integratedTime);

            // Apply impulses to hit bodies
            if (affectBodies)
            {
                CalculateAndStoreDeferredImpulses(stepInput, characterMass, prevVelocity, ref constraints, ref deferredImpulseWriter);
            }

            // Calculate new displacement
            float3 newDisplacement = newPosition - prevPosition;

            // If simplex solver moved the character we need to re-cast to make sure it can move to new position
            if (math.lengthsq(newDisplacement) > k_SimplexSolverEpsilon)
            {
                // Check if we can walk to the position simplex solver has suggested
                var newCollector = new SelfFilteringClosestHitCollector<ColliderCastHit>(stepInput.RigidBodyIndex, 1.0f);

                ColliderCastInput input = new ColliderCastInput()
                {
                    Collider = collider,
                    Orientation = orientation,
                    Start = prevPosition,
                    End = prevPosition + newDisplacement
                };

                world.CastCollider(input, ref newCollector);

                if (newCollector.NumHits > 0)
                {
                    ColliderCastHit hit = newCollector.ClosestHit;

                    bool found = false;
                    for (int constraintIndex = 0; constraintIndex < constraints.Length; constraintIndex++)
                    {
                        SurfaceConstraintInfo constraint = constraints[constraintIndex];
                        if (constraint.RigidBodyIndex == hit.RigidBodyIndex &&
                            constraint.ColliderKey.Equals(hit.ColliderKey))
                        {
                            found = true;
                            break;
                        }
                    }

                    // Move character along the newDisplacement direction until it reaches this new contact
                    if (!found)
                    {
                        Assert.IsTrue(hit.Fraction >= 0.0f && hit.Fraction <= 1.0f);

                        integratedTime *= hit.Fraction;
                        newPosition = prevPosition + newDisplacement * hit.Fraction;
                    }
                }
            }

            // Reduce remaining time
            remainingTime -= integratedTime;
        }

        // Write back position and velocity
        transform.pos = newPosition;
        linearVelocity = newVelocity;
    }

    private static void CreateConstraintFromHit(PhysicsWorld world, int rigidBodyIndex, ColliderKey colliderKey,
        float3 hitPosition, float3 normal, float distance, float skinWidth, out SurfaceConstraintInfo constraint)
    {
        bool bodyIsDynamic = 0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies;
        constraint = new SurfaceConstraintInfo()
        {
            Plane = new Plane
            {
                Normal = normal,
                Distance = distance - skinWidth,
            },
            RigidBodyIndex = rigidBodyIndex,
            ColliderKey = colliderKey,
            HitPosition = hitPosition,
            Velocity = bodyIsDynamic ?
                world.GetLinearVelocity(rigidBodyIndex, hitPosition) :
                float3.zero,
            Priority = bodyIsDynamic ? 1 : 0
        };
    }

    private static void CreateMaxSlopeConstraint(float3 up, ref SurfaceConstraintInfo constraint, out SurfaceConstraintInfo maxSlopeConstraint)
    {
        float verticalComponent = math.dot(constraint.Plane.Normal, up);

        SurfaceConstraintInfo newConstraint = constraint;
        newConstraint.Plane.Normal = math.normalize(newConstraint.Plane.Normal - verticalComponent * up);

        float distance = newConstraint.Plane.Distance;

        // Calculate distance to the original plane along the new normal.
        // Clamp the new distance to 2x the old distance to avoid penetration recovery explosions.
        newConstraint.Plane.Distance = distance / math.max(math.dot(newConstraint.Plane.Normal, constraint.Plane.Normal), 0.5f);

        if (newConstraint.Plane.Distance < 0.0f)
        {
            // Disable penetration recovery for the original plane
            constraint.Plane.Distance = 0.0f;

            // Prepare velocity to resolve penetration
            ResolveConstraintPenetration(ref newConstraint);
        }

        // Output max slope constraint
        maxSlopeConstraint = newConstraint;
    }

    private static void ResolveConstraintPenetration(ref SurfaceConstraintInfo constraint)
    {
        // Fix up the velocity to enable penetration recovery
        if (constraint.Plane.Distance < 0.0f)
        {
            float3 newVel = constraint.Velocity - constraint.Plane.Normal * constraint.Plane.Distance;
            constraint.Velocity = newVel;
            constraint.Plane.Distance = 0.0f;
        }
    }

    private static void CreateConstraint(PhysicsWorld world, float3 up,
        int hitRigidBodyIndex, ColliderKey hitColliderKey, float3 hitPosition, float3 hitSurfaceNormal, float hitDistance,
        float skinWidth, float maxSlopeCos, ref NativeList<SurfaceConstraintInfo> constraints)
    {
        CreateConstraintFromHit(world, hitRigidBodyIndex, hitColliderKey, hitPosition,
            hitSurfaceNormal, hitDistance, skinWidth, out SurfaceConstraintInfo constraint);

        // Check if max slope plane is required
        float verticalComponent = math.dot(constraint.Plane.Normal, up);
        bool shouldAddPlane = verticalComponent > k_SimplexSolverEpsilon && verticalComponent < maxSlopeCos;
        if (shouldAddPlane)
        {
            constraint.IsTooSteep = true;
            CreateMaxSlopeConstraint(up, ref constraint, out SurfaceConstraintInfo maxSlopeConstraint);
            constraints.Add(maxSlopeConstraint);
        }

        // Prepare velocity to resolve penetration
        ResolveConstraintPenetration(ref constraint);

        // Add original constraint to the list
        constraints.Add(constraint);
    }

    private static unsafe void CalculateAndStoreDeferredImpulses(
        CharacterControllerStepInput stepInput, float characterMass, float3 linearVelocity,
        ref NativeList<SurfaceConstraintInfo> constraints, ref NativeStream.Writer deferredImpulseWriter)
    {
        PhysicsWorld world = stepInput.World;
        for (int i = 0; i < constraints.Length; i++)
        {
            SurfaceConstraintInfo constraint = constraints[i];

            int rigidBodyIndex = constraint.RigidBodyIndex;
            if (rigidBodyIndex < 0 || rigidBodyIndex >= world.NumDynamicBodies)
            {
                // Invalid and static bodies should be skipped
                continue;
            }

            RigidBody body = world.Bodies[rigidBodyIndex];

            float3 pointRelVel = world.GetLinearVelocity(rigidBodyIndex, constraint.HitPosition);
            pointRelVel -= linearVelocity;

            float projectedVelocity = math.dot(pointRelVel, constraint.Plane.Normal);

            // Required velocity change
            float deltaVelocity = - projectedVelocity * stepInput.Damping;

            float distance = constraint.Plane.Distance;
            if (distance < 0.0f)
            {
                deltaVelocity += (distance / stepInput.DeltaTime) * stepInput.Tau;
            }

            // Calculate impulse
            MotionVelocity mv = world.MotionVelocities[rigidBodyIndex];
            float3 impulse = float3.zero;
            if (deltaVelocity < 0.0f)
            {
                // Impulse magnitude
                float impulseMagnitude = 0.0f;
                {
                    float objectMassInv = GetInvMassAtPoint(constraint.HitPosition, constraint.Plane.Normal, body, mv);
                    impulseMagnitude = deltaVelocity / objectMassInv;
                }

                impulse = impulseMagnitude * constraint.Plane.Normal;
            }

            // Add gravity
            {
                // Effect of gravity on character velocity in the normal direction
                float3 charVelDown = stepInput.Gravity * stepInput.DeltaTime;
                float relVelN = math.dot(charVelDown, constraint.Plane.Normal);

                // Subtract separation velocity if separating contact
                {
                    bool isSeparatingContact = projectedVelocity < 0.0f;
                    float newRelVelN = relVelN - projectedVelocity;
                    relVelN = math.select(relVelN, newRelVelN, isSeparatingContact);
                }

                // If resulting velocity is negative, an impulse is applied to stop the character
                // from falling into the body
                {
                    float3 newImpulse = impulse;
                    newImpulse += relVelN * characterMass * constraint.Plane.Normal;
                    impulse = math.select(impulse, newImpulse, relVelN < 0.0f);
                }
            }

            // Store impulse
            deferredImpulseWriter.Write(
                new DeferredCharacterControllerImpulse()
                {
                    Entity = body.Entity,
                    Impulse = impulse,
                    Point = constraint.HitPosition
                });
        }
    }

    private static float GetInvMassAtPoint(float3 point, float3 normal, RigidBody body, MotionVelocity mv)
    {
        float3 massCenter;
        unsafe
        {
            massCenter = math.transform(body.WorldFromBody, body.Collider->MassProperties.MassDistribution.Transform.pos);
        }
        float3 arm = point - massCenter;
        float3 jacAng = math.cross(arm, normal);
        float3 armC = jacAng * mv.InverseInertiaAndMass.xyz;

        float objectMassInv = math.dot(armC, jacAng);
        objectMassInv += mv.InverseInertiaAndMass.w;

        return objectMassInv;
    }
}

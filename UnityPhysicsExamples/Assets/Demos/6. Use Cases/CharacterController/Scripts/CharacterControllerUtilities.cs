using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using UnityEngine.Assertions;

public struct DeferredCharacterControllerImpulse
{
    public Entity Entity;
    public float3 Impulse;
    public float3 Point;
}

public static class CharacterControllerUtilities
{
    public enum CharacterSupportState : byte
    {
        Unsupported = 0,
        Sliding,
        Supported
    }

    // A collector which stores every hit up to the length of the provided native array.
    public struct MaxHitsCollector<T> : ICollector<T> where T : struct, IQueryResult
    {
        private int m_NumHits;
        public bool EarlyOutOnFirstHit => false;
        public float MaxFraction { get; }
        public int NumHits => m_NumHits;

        public NativeArray<T> AllHits;

        public MaxHitsCollector(float maxFraction, ref NativeArray<T> allHits)
        {
            MaxFraction = maxFraction;
            AllHits = allHits;
            m_NumHits = 0;
        }

        #region IQueryResult implementation

        public bool AddHit(T hit)
        {
            Assert.IsTrue(hit.Fraction < MaxFraction);
            Assert.IsTrue(m_NumHits < AllHits.Length);
            AllHits[m_NumHits] = hit;
            m_NumHits++;
            return true;
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, uint numSubKeyBits, uint subKey)
        {
            for (int i = oldNumHits; i < m_NumHits; i++)
            {
                T hit = AllHits[i];
                hit.Transform(transform, numSubKeyBits, subKey);
                AllHits[i] = hit;
            }
        }

        public void TransformNewHits(int oldNumHits, float oldFraction, Math.MTransform transform, int rigidBodyIndex)
        {
            for (int i = oldNumHits; i < m_NumHits; i++)
            {
                T hit = AllHits[i];
                hit.Transform(transform, rigidBodyIndex);
                AllHits[i] = hit;
            }
        }

        #endregion
    }

    public static unsafe void CheckSupport(PhysicsWorld world, float deltaTime, RigidTransform transform,
        float3 downwardsDirection, float maxSlope, float contactTolerance, Collider* collider, ref NativeArray<SurfaceConstraintInfo> constraints,
        ref NativeArray<DistanceHit> checkSupportHits, out CharacterSupportState characterState)
    {
        // Downwards direction must be normalized
        Assert.IsTrue(Math.IsNormalized(downwardsDirection));

        // "Broad phase"
        MaxHitsCollector<DistanceHit> collector = new MaxHitsCollector<DistanceHit>(contactTolerance, ref checkSupportHits);
        {
            ColliderDistanceInput input = new ColliderDistanceInput()
            {
                MaxDistance = contactTolerance,
                Transform = transform,
                Collider = collider
            };
            world.CalculateDistance(input, ref collector);
        }

        // Iterate over hits and create constraints from them
        for (int i = 0; i < collector.NumHits; i++)
        {
            DistanceHit hit = collector.AllHits[i];
            CreateConstraintFromHit(world, float3.zero, deltaTime, hit.RigidBodyIndex, hit.ColliderKey,
                hit.Position, hit.SurfaceNormal, hit.Distance, true, out SurfaceConstraintInfo constraint);
            constraints[i] = constraint;
        }

        // Solve downwards
        float3 outVelocity = downwardsDirection;
        float3 outPosition = transform.pos;
        SimplexSolver.Solve(world, deltaTime, -downwardsDirection, collector.NumHits, ref constraints, ref outPosition, ref outVelocity, out float integratedTime);

        // If no hits, proclaim unsupported state
        if (collector.NumHits == 0)
        {
            characterState = CharacterSupportState.Unsupported;
        }
        else
        {
            if (math.lengthsq(downwardsDirection - outVelocity) < SimplexSolver.c_SimplexSolverEpsilon)
            {
                // If velocity hasn't changed significantly, declare unsupported state
                characterState = CharacterSupportState.Unsupported;
            }
            else if (math.lengthsq(outVelocity) < SimplexSolver.c_SimplexSolverEpsilon)
            {
                // If velocity is very small, declare supported state
                characterState = CharacterSupportState.Supported;
            }
            else
            {
                // Check if sliding or supported
                outVelocity = math.normalize(outVelocity);
                float slopeAngleSin = math.dot(outVelocity, downwardsDirection);
                float slopeAngleCosSq = 1 - slopeAngleSin * slopeAngleSin;
                float maxSlopeCosine = math.cos(maxSlope);
                if (slopeAngleCosSq < maxSlopeCosine * maxSlopeCosine)
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

    public static unsafe void CollideAndIntegrate(PhysicsWorld world, float deltaTime,
        int maxIterations, float3 up, float3 gravity,
        float characterMass, float tau, float damping,  float maxSlope, bool affectBodies, Collider* collider,
        ref NativeArray<DistanceHit> distanceHits, ref NativeArray<ColliderCastHit> castHits, ref NativeArray<SurfaceConstraintInfo> constraints,
        ref RigidTransform transform, ref float3 linearVelocity, ref BlockStream.Writer deferredImpulseWriter)
    {
        float remainingTime = deltaTime;
        float3 lastDisplacement = linearVelocity * remainingTime;

        float3 newPosition = transform.pos;
        quaternion orientation = transform.rot;
        float3 newVelocity = linearVelocity;

        float maxSlopeCos = math.cos(maxSlope);

        const float timeEpsilon = 0.000001f;
        for (int i = 0; i < maxIterations && remainingTime > timeEpsilon; i++)
        {
            // First do distance query for penetration recovery
            MaxHitsCollector<DistanceHit> distanceHitsCollector = new MaxHitsCollector<DistanceHit>(0.0f, ref distanceHits);
            int numConstraints = 0;
            {
                ColliderDistanceInput input = new ColliderDistanceInput()
                {
                    MaxDistance = 0.0f,
                    Transform = new RigidTransform
                    {
                        pos = newPosition,
                        rot = orientation,
                    },
                    Collider = collider
                };
                world.CalculateDistance(input, ref distanceHitsCollector);

                // Iterate over hits and create constraints from them
                for (int hitIndex = 0; hitIndex < distanceHitsCollector.NumHits; hitIndex++)
                {
                    DistanceHit hit = distanceHitsCollector.AllHits[hitIndex];
                    CreateConstraintFromHit(world, gravity, deltaTime, hit.RigidBodyIndex, hit.ColliderKey, hit.Position,
                        hit.SurfaceNormal, hit.Distance, false, out SurfaceConstraintInfo constraint);

                    // Potentially add a max slope constraint
                    AddMaxSlopeConstraint(up, maxSlopeCos, ref constraint, ref constraints, ref numConstraints);

                    // Add original constraint to the list
                    constraints[numConstraints++] = constraint;
                }
            }

            float3 gravityMovement = gravity * remainingTime * remainingTime * 0.5f;

            // Then do a collider cast
            {
                float3 displacement = lastDisplacement + gravityMovement;
                MaxHitsCollector<ColliderCastHit> collector = new MaxHitsCollector<ColliderCastHit>(1.0f, ref castHits);
                ColliderCastInput input = new ColliderCastInput()
                {
                    Collider = collider,
                    Orientation = orientation,
                    Start = newPosition,
                    End = newPosition + displacement,
                };
                world.CastCollider(input, ref collector);

                // Iterate over hits and create constraints from them
                for (int hitIndex = 0; hitIndex < collector.NumHits; hitIndex++)
                {
                    ColliderCastHit hit = collector.AllHits[hitIndex];

                    bool found = false;
                    for (int distanceHitIndex = 0; distanceHitIndex < distanceHitsCollector.NumHits; distanceHitIndex++)
                    {
                        DistanceHit dHit = distanceHitsCollector.AllHits[distanceHitIndex];
                        if (dHit.RigidBodyIndex == hit.RigidBodyIndex &&
                            dHit.ColliderKey.Equals(hit.ColliderKey))
                        {
                            found = true;
                            break;
                        }
                    }

                    // Skip duplicate hits
                    if (!found)
                    {
                        CreateConstraintFromHit(world, gravity, deltaTime, hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal,
                            hit.Fraction * math.length(lastDisplacement), false, out SurfaceConstraintInfo constraint);

                        // Potentially add a max slope constraint
                        AddMaxSlopeConstraint(up, maxSlopeCos, ref constraint, ref constraints, ref numConstraints);

                        // Add original constraint to the list
                        constraints[numConstraints++] = constraint;
                    }
                }
            }

            // Solve
            float3 prevVelocity = newVelocity;
            float3 prevPosition = newPosition;
            SimplexSolver.Solve(world, remainingTime, up, numConstraints, ref constraints, ref newPosition, ref newVelocity, out float integratedTime);

            // Apply impulses to hit bodies
            if (affectBodies)
            {
                ResolveContacts(world, remainingTime, gravity, tau, damping, characterMass, prevVelocity, numConstraints, ref constraints, ref deferredImpulseWriter);
            }

            float3 newDisplacement = newPosition - prevPosition;

            // Check if we can walk to the position simplex solver has suggested
            MaxHitsCollector<ColliderCastHit> newCollector = new MaxHitsCollector<ColliderCastHit>(1.0f, ref castHits);
            int newContactIndex = -1;

            // If simplex solver moved the character we need to re-cast to make sure it can move to new position
            if (math.lengthsq(newDisplacement) > SimplexSolver.c_SimplexSolverEpsilon)
            {
                float3 displacement = newDisplacement + gravityMovement;
                ColliderCastInput input = new ColliderCastInput()
                {
                    Collider = collider,
                    Orientation = orientation,
                    Start = prevPosition,
                    End = prevPosition + displacement
                };

                world.CastCollider(input, ref newCollector);

                for (int hitIndex = 0; hitIndex < newCollector.NumHits; hitIndex++)
                {
                    ColliderCastHit hit = newCollector.AllHits[hitIndex];

                    bool found = false;
                    for (int constraintIndex = 0; constraintIndex < numConstraints; constraintIndex++)
                    {
                        SurfaceConstraintInfo constraint = constraints[constraintIndex];
                        if (constraint.RigidBodyIndex == hit.RigidBodyIndex &&
                            constraint.ColliderKey.Equals(hit.ColliderKey))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        newContactIndex = hitIndex;
                        break;
                    }
                }
            }

            // Move character along the newDisplacement direction until it reaches this new contact
            if (newContactIndex >= 0)
            {
                ColliderCastHit newContact = newCollector.AllHits[newContactIndex];

                float fraction = newContact.Fraction / math.length(newDisplacement);
                integratedTime *= fraction;

                float3 displacement = newDisplacement * fraction;
                newPosition = prevPosition + displacement;
            }

            remainingTime -= integratedTime;

            // Remember last displacement for next iteration
            lastDisplacement = newVelocity * remainingTime;
        }

        // Write back position and velocity
        transform.pos = newPosition;
        linearVelocity = newVelocity;
    }

    private static void CreateConstraintFromHit(PhysicsWorld world, float3 gravity, float deltaTime, int rigidBodyIndex, ColliderKey colliderKey,
        float3 hitPosition, float3 normal, float distance, bool zeroVelocity, out SurfaceConstraintInfo constraint)
    {
        bool bodyIsDynamic = 0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies;
        constraint = new SurfaceConstraintInfo()
        {
            Plane = new Plane
            {
                Normal = normal,
                Distance = distance,
            },
            RigidBodyIndex = rigidBodyIndex,
            ColliderKey = colliderKey,
            HitPosition = hitPosition,
            Velocity = bodyIsDynamic && !zeroVelocity ?
                world.MotionVelocities[rigidBodyIndex].LinearVelocity - world.MotionDatas[rigidBodyIndex].GravityFactor * gravity * deltaTime :
                constraint.Velocity = float3.zero,
            Priority = bodyIsDynamic ? 1 : 0
        };

        // Fix up the velocity to enable penetration recovery
        if (distance < 0.0f)
        {
            float3 newVel = constraint.Velocity - constraint.Plane.Normal * distance;
            constraint.Velocity = newVel;
        }
    }

    private static void AddMaxSlopeConstraint(float3 up, float maxSlopeCos, ref SurfaceConstraintInfo constraint, ref NativeArray<SurfaceConstraintInfo> constraints, ref int numConstraints)
    {
        // Check if max slope plane is required
        float verticalComponent = math.dot(constraint.Plane.Normal, up);
        bool shouldAddPlane = verticalComponent > SimplexSolver.c_SimplexSolverEpsilon && verticalComponent < maxSlopeCos;
        if (shouldAddPlane)
        {
            SurfaceConstraintInfo newConstraint = constraint;
            newConstraint.Plane.Normal = math.normalize(newConstraint.Plane.Normal - verticalComponent * up);

            float distance = newConstraint.Plane.Distance;

            // Calculate distance to the original plane along the new normal
            newConstraint.Plane.Distance = distance / math.dot(newConstraint.Plane.Normal, constraint.Plane.Normal);

            if (distance < 0.0f)
            {
                // Disable penetration recovery for the original plane
                constraint.Plane.Distance = 0.0f;

                // Set the new constraint velocity
                float3 newVel = newConstraint.Velocity - newConstraint.Plane.Normal * distance;
                newConstraint.Velocity = newVel;
            }

            // Add max slope constraint to the list
            constraints[numConstraints++] = newConstraint;
        }
    }

    private static unsafe void ResolveContacts(PhysicsWorld world, float deltaTime, float3 gravity, float tau, float damping,
        float characterMass, float3 linearVelocity, int numConstraints, ref NativeArray<SurfaceConstraintInfo> constraints, ref BlockStream.Writer deferredImpulseWriter)
    {
        for (int i = 0; i < numConstraints; i++)
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
            float deltaVelocity = - projectedVelocity * damping;

            float distance = constraint.Plane.Distance;
            if (distance < 0.0f)
            {
                deltaVelocity += (distance / deltaTime) * tau;
            }

            // Calculate impulse
            MotionVelocity mv = world.MotionVelocities[rigidBodyIndex];
            float3 impulse = float3.zero;
            if (deltaVelocity < 0.0f)
            {
                // Impulse magnitude
                float impulseMagnitude = 0.0f;
                {
                    float3 arm = constraint.HitPosition - (body.WorldFromBody.pos + body.Collider->MassProperties.MassDistribution.Transform.pos);
                    float3 jacAng = math.cross(arm, constraint.Plane.Normal);
                    float3 armC = jacAng * mv.InverseInertiaAndMass.xyz;

                    float objectMassInv = math.dot(armC, jacAng);
                    objectMassInv += mv.InverseInertiaAndMass.w;
                    impulseMagnitude = deltaVelocity / objectMassInv;
                }

                impulse = impulseMagnitude * constraint.Plane.Normal;
            }

            // Add gravity
            {
                // Effect of gravity on character velocity in the normal direction
                float3 charVelDown = gravity * deltaTime;
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
}

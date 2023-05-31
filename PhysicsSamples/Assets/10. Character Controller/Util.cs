using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Stateful;
using UnityEngine.Assertions;

namespace CharacterController
{
    // Stores the impulse to be applied by the character controller body
    public struct DeferredCharacterImpulse
    {
        public Entity Entity;
        public float3 Impulse;
        public float3 Point;
    }

    public static class Util
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

        public struct StepInput
        {
            public PhysicsWorldSingleton PhysicsWorldSingleton;
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

        public struct AllHitsCollector<T> : ICollector<T> where T : unmanaged, IQueryResult
        {
            private int m_selfRBIndex;

            public bool EarlyOutOnFirstHit => false;
            public float MaxFraction { get; }
            public int NumHits => AllHits.Length;

            public float MinHitFraction;
            public NativeList<T> AllHits;
            public NativeList<T> TriggerHits;

            private PhysicsWorld m_world;

            public AllHitsCollector(int rbIndex, float maxFraction, ref NativeList<T> allHits,
                                    PhysicsWorldSingleton physicsWorldSingleton,
                                    NativeList<T> triggerHits = default)
            {
                MaxFraction = maxFraction;
                AllHits = allHits;
                m_selfRBIndex = rbIndex;
                m_world = physicsWorldSingleton.PhysicsWorld;
                TriggerHits = triggerHits;
                MinHitFraction = float.MaxValue;
            }

            public AllHitsCollector(int rbIndex, float maxFraction, ref NativeList<T> allHits, PhysicsWorld world,
                                    NativeList<T> triggerHits = default)
            {
                MaxFraction = maxFraction;
                AllHits = allHits;
                m_selfRBIndex = rbIndex;
                m_world = world;
                TriggerHits = triggerHits;
                MinHitFraction = float.MaxValue;
            }

            #region ICollector

            public bool AddHit(T hit)
            {
                Assert.IsTrue(hit.Fraction <= MaxFraction);

                if (hit.RigidBodyIndex == m_selfRBIndex)
                {
                    return false;
                }

                if (hit.Material.CollisionResponse == CollisionResponsePolicy.RaiseTriggerEvents)
                {
                    if (TriggerHits.IsCreated)
                    {
                        TriggerHits.Add(hit);
                    }

                    return false;
                }

                MinHitFraction = math.min(MinHitFraction, hit.Fraction);
                AllHits.Add(hit);
                return true;
            }

            #endregion
        }

        // A collector which stores only the closest hit different from itself, the triggers, and predefined list of values it hit.
        public struct ClosestHitCollector<T> : ICollector<T> where T : struct, IQueryResult
        {
            public bool EarlyOutOnFirstHit => false;
            public float MaxFraction { get; private set; }
            public int NumHits { get; private set; }

            private T m_ClosestHit;
            public T ClosestHit => m_ClosestHit;

            private int m_selfRBIndex;
            private PhysicsWorld m_world;

            private NativeList<SurfaceConstraintInfo> m_PredefinedConstraints;

            public ClosestHitCollector(NativeList<SurfaceConstraintInfo> predefinedConstraints,
                                       PhysicsWorld world, int rbIndex, float maxFraction)
            {
                MaxFraction = maxFraction;
                m_ClosestHit = default;
                NumHits = 0;
                m_selfRBIndex = rbIndex;
                m_world = world;
                m_PredefinedConstraints = predefinedConstraints;
            }

            public ClosestHitCollector(NativeList<SurfaceConstraintInfo> predefinedConstraints,
                                       PhysicsWorldSingleton world, int rbIndex, float maxFraction)
            {
                MaxFraction = maxFraction;
                m_ClosestHit = default;
                NumHits = 0;
                m_selfRBIndex = rbIndex;
                m_world = world.PhysicsWorld;
                m_PredefinedConstraints = predefinedConstraints;
            }

            #region ICollector

            public bool AddHit(T hit)
            {
                Assert.IsTrue(hit.Fraction <= MaxFraction);

                // Check self hits and trigger hits
                if ((hit.RigidBodyIndex == m_selfRBIndex) ||
                    (hit.Material.CollisionResponse == CollisionResponsePolicy.RaiseTriggerEvents))
                {
                    return false;
                }

                // Check predefined hits
                for (int i = 0; i < m_PredefinedConstraints.Length; i++)
                {
                    SurfaceConstraintInfo constraint = m_PredefinedConstraints[i];
                    if (constraint.RigidBodyIndex == hit.RigidBodyIndex &&
                        constraint.ColliderKey.Equals(hit.ColliderKey))
                    {
                        // Hit was already defined, skip it
                        return false;
                    }
                }

                // Finally, accept the hit
                MaxFraction = hit.Fraction;
                m_ClosestHit = hit;
                NumHits = 1;
                return true;
            }

            #endregion
        }

        public static void CheckSupport(
            in PhysicsWorldSingleton physicsWorldSingleton, ref PhysicsCollider collider, StepInput stepInput,
            RigidTransform transform,
            out CharacterSupportState characterState, out float3 surfaceNormal, out float3 surfaceVelocity,
            NativeList<StatefulCollisionEvent> collisionEvents = default)
        {
            surfaceNormal = float3.zero;
            surfaceVelocity = float3.zero;

            // Up direction must be normalized
            Assert.IsTrue(Unity.Physics.Math.IsNormalized(stepInput.Up));

            // Query the world
            NativeList<ColliderCastHit> castHits =
                new NativeList<ColliderCastHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
            AllHitsCollector<ColliderCastHit> castHitsCollector = new AllHitsCollector<ColliderCastHit>(
                stepInput.RigidBodyIndex, 1.0f, ref castHits, physicsWorldSingleton);
            var maxDisplacement = -stepInput.ContactTolerance * stepInput.Up;
            {
                ColliderCastInput input = new ColliderCastInput(collider.Value, transform.pos,
                    transform.pos + maxDisplacement, transform.rot);

                physicsWorldSingleton.PhysicsWorld.CastCollider(input, ref castHitsCollector);
            }

            // If no hits, proclaim unsupported state
            if (castHitsCollector.NumHits == 0)
            {
                characterState = CharacterSupportState.Unsupported;
                return;
            }

            float maxSlopeCos = math.cos(stepInput.MaxSlope);

            // Iterate over distance hits and create constraints from them
            NativeList<SurfaceConstraintInfo> constraints =
                new NativeList<SurfaceConstraintInfo>(k_DefaultConstraintsCapacity, Allocator.Temp);
            float maxDisplacementLength = math.length(maxDisplacement);
            for (int i = 0; i < castHitsCollector.NumHits; i++)
            {
                ColliderCastHit hit = castHitsCollector.AllHits[i];
                CreateConstraint(stepInput.PhysicsWorldSingleton.PhysicsWorld, stepInput.Up,
                    hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal,
                    hit.Fraction * maxDisplacementLength,
                    stepInput.SkinWidth, maxSlopeCos, ref constraints);
            }

            // Velocity for support checking
            float3 initialVelocity = maxDisplacement / stepInput.DeltaTime;
            Math.ClampToMaxLength(stepInput.MaxMovementSpeed, ref initialVelocity);

            // Solve downwards (don't use min delta time, try to solve full step)
            float3 outVelocity = initialVelocity;
            float3 outPosition = transform.pos;
            SimplexSolver.Solve(stepInput.DeltaTime, stepInput.DeltaTime, stepInput.Up, stepInput.MaxMovementSpeed,
                constraints, ref outPosition, ref outVelocity, out float integratedTime, false);

            // Get info on surface
            int numSupportingPlanes = 0;
            {
                for (int j = 0; j < constraints.Length; j++)
                {
                    var constraint = constraints[j];
                    if (constraint.Touched && !constraint.IsTooSteep && !constraint.IsMaxSlope)
                    {
                        numSupportingPlanes++;
                        surfaceNormal += constraint.Plane.Normal;
                        surfaceVelocity += constraint.Velocity;

                        // Add supporting planes to collision events
                        if (collisionEvents.IsCreated)
                        {
                            CollisionWorld world = stepInput.PhysicsWorldSingleton.PhysicsWorld.CollisionWorld;
                            var collisionEvent = new StatefulCollisionEvent()
                            {
                                EntityA = world.Bodies[stepInput.RigidBodyIndex].Entity,
                                EntityB = world.Bodies[constraint.RigidBodyIndex].Entity,
                                BodyIndexA = stepInput.RigidBodyIndex,
                                BodyIndexB = constraint.RigidBodyIndex,
                                ColliderKeyA = ColliderKey.Empty,
                                ColliderKeyB = constraint.ColliderKey,
                                Normal = constraint.Plane.Normal
                            };
                            collisionEvent.CollisionDetails =
                                new StatefulCollisionEvent.Details(1, 0, constraint.HitPosition);
                            collisionEvents.Add(collisionEvent);
                        }
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
                else if (math.lengthsq(outVelocity) < k_SimplexSolverEpsilonSq && numSupportingPlanes > 0)
                {
                    // If velocity is very small, declare supported state
                    characterState = CharacterSupportState.Supported;
                }
                else
                {
                    // Check if sliding
                    outVelocity = math.normalize(outVelocity);
                    float slopeAngleSin = math.max(0.0f, math.dot(outVelocity, -stepInput.Up));
                    float slopeAngleCosSq = 1 - slopeAngleSin * slopeAngleSin;
                    if (slopeAngleCosSq <= maxSlopeCos * maxSlopeCos)
                    {
                        characterState = CharacterSupportState.Sliding;
                    }
                    else if (numSupportingPlanes > 0)
                    {
                        characterState = CharacterSupportState.Supported;
                    }
                    else
                    {
                        // If numSupportingPlanes is 0, surface normal is invalid, so state is unsupported
                        characterState = CharacterSupportState.Unsupported;
                    }
                }
            }
        }

        public static void CollideAndIntegrate(
            StepInput stepInput, float characterMass, bool affectBodies, ref PhysicsCollider collider,
            ref RigidTransform transform, ref float3 linearVelocity, ref NativeStream.Writer deferredImpulseWriter,
            NativeList<StatefulCollisionEvent> collisionEvents = default,
            NativeList<StatefulTriggerEvent> triggerEvents = default)
        {
            // Copy parameters
            float deltaTime = stepInput.DeltaTime;
            float3 up = stepInput.Up;
            PhysicsWorld world = stepInput.PhysicsWorldSingleton.PhysicsWorld;

            float remainingTime = deltaTime;

            float3 newPosition = transform.pos;
            quaternion orientation = transform.rot;
            float3 newVelocity = linearVelocity;

            float maxSlopeCos = math.cos(stepInput.MaxSlope);

            const float timeEpsilon = 0.000001f;
            for (int i = 0; i < stepInput.MaxIterations && remainingTime > timeEpsilon; i++)
            {
                NativeList<SurfaceConstraintInfo> constraints =
                    new NativeList<SurfaceConstraintInfo>(k_DefaultConstraintsCapacity, Allocator.Temp);

                // Do a collider cast
                {
                    float3 displacement = newVelocity * remainingTime;
                    NativeList<ColliderCastHit> triggerHits = default;
                    if (triggerEvents.IsCreated)
                    {
                        triggerHits = new NativeList<ColliderCastHit>(k_DefaultQueryHitsCapacity / 4, Allocator.Temp);
                    }

                    NativeList<ColliderCastHit> castHits =
                        new NativeList<ColliderCastHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
                    AllHitsCollector<ColliderCastHit> collector = new AllHitsCollector<ColliderCastHit>(
                        stepInput.RigidBodyIndex, 1.0f, ref castHits, stepInput.PhysicsWorldSingleton, triggerHits);
                    ColliderCastInput input = new ColliderCastInput(collider.Value, newPosition,
                        newPosition + displacement, orientation);
                    stepInput.PhysicsWorldSingleton.PhysicsWorld.CastCollider(input, ref collector);

                    // Iterate over hits and create constraints from them
                    for (int hitIndex = 0; hitIndex < collector.NumHits; hitIndex++)
                    {
                        ColliderCastHit hit = collector.AllHits[hitIndex];
                        CreateConstraint(stepInput.PhysicsWorldSingleton.PhysicsWorld, stepInput.Up,
                            hit.RigidBodyIndex, hit.ColliderKey, hit.Position, hit.SurfaceNormal,
                            math.dot(-hit.SurfaceNormal, hit.Fraction * displacement),
                            stepInput.SkinWidth, maxSlopeCos, ref constraints);
                    }

                    // Update trigger events
                    if (triggerEvents.IsCreated)
                    {
                        UpdateTriggersSeen(stepInput, triggerHits, triggerEvents, collector.MinHitFraction);
                    }
                }

                // Then do a collider distance for penetration recovery,
                // but only fix up penetrating hits
                {
                    // Collider distance query
                    NativeList<DistanceHit> distanceHits =
                        new NativeList<DistanceHit>(k_DefaultQueryHitsCapacity, Allocator.Temp);
                    AllHitsCollector<DistanceHit> distanceHitsCollector = new AllHitsCollector<DistanceHit>(
                        stepInput.RigidBodyIndex, stepInput.ContactTolerance, ref distanceHits,
                        stepInput.PhysicsWorldSingleton);
                    {
                        ColliderDistanceInput input =
                            new ColliderDistanceInput(collider.Value, stepInput.ContactTolerance, transform);
                        stepInput.PhysicsWorldSingleton.PhysicsWorld.CalculateDistance(input,
                            ref distanceHitsCollector);
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
                                        CreateConstraintFromHit(stepInput.PhysicsWorldSingleton.PhysicsWorld,
                                            hit.RigidBodyIndex, hit.ColliderKey,
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
                                CreateConstraint(stepInput.PhysicsWorldSingleton.PhysicsWorld, stepInput.Up,
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
                SimplexSolver.Solve(remainingTime, minDeltaTime, up, stepInput.MaxMovementSpeed, constraints,
                    ref newPosition, ref newVelocity, out float integratedTime);

                // Apply impulses to hit bodies and store collision events
                if (affectBodies || collisionEvents.IsCreated)
                {
                    CalculateAndStoreDeferredImpulsesAndCollisionEvents(stepInput, affectBodies, characterMass,
                        prevVelocity, constraints, ref deferredImpulseWriter, collisionEvents);
                }

                // Calculate new displacement
                float3 newDisplacement = newPosition - prevPosition;

                // If simplex solver moved the character we need to re-cast to make sure it can move to new position
                if (math.lengthsq(newDisplacement) > k_SimplexSolverEpsilon)
                {
                    // Check if we can walk to the position simplex solver has suggested
                    var newCollector = new ClosestHitCollector<ColliderCastHit>(constraints,
                        stepInput.PhysicsWorldSingleton, stepInput.RigidBodyIndex, 1.0f);

                    ColliderCastInput input = new ColliderCastInput(collider.Value, prevPosition,
                        prevPosition + newDisplacement, orientation);

                    stepInput.PhysicsWorldSingleton.PhysicsWorld.CastCollider(input, ref newCollector);

                    if (newCollector.NumHits > 0)
                    {
                        ColliderCastHit hit = newCollector.ClosestHit;

                        // Move character along the newDisplacement direction until it reaches this new contact
                        {
                            Assert.IsTrue(hit.Fraction >= 0.0f && hit.Fraction <= 1.0f);

                            integratedTime *= hit.Fraction;
                            newPosition = prevPosition + newDisplacement * hit.Fraction;
                        }
                    }
                }

                // Reduce remaining time
                remainingTime -= integratedTime;

                // Write back position so that the distance query will update results
                transform.pos = newPosition;
            }

            // Write back final velocity
            linearVelocity = newVelocity;
        }

        private static void CreateConstraintFromHit(PhysicsWorld world, int rigidBodyIndex, ColliderKey colliderKey,
            float3 hitPosition, float3 normal, float distance, float skinWidth, out SurfaceConstraintInfo constraint)
        {
            bool bodyIsDynamic = 0 <= rigidBodyIndex && rigidBodyIndex < world.NumDynamicBodies;
            constraint = new SurfaceConstraintInfo()
            {
                Plane = new Unity.Physics.Plane
                {
                    Normal = normal,
                    Distance = distance - skinWidth,
                },
                RigidBodyIndex = rigidBodyIndex,
                ColliderKey = colliderKey,
                HitPosition = hitPosition,
                Velocity = bodyIsDynamic ? world.GetLinearVelocity(rigidBodyIndex, hitPosition) : float3.zero,
                Priority = bodyIsDynamic ? 1 : 0
            };
        }

        private static void CreateMaxSlopeConstraint(float3 up, ref SurfaceConstraintInfo constraint,
            out SurfaceConstraintInfo maxSlopeConstraint)
        {
            float verticalComponent = math.dot(constraint.Plane.Normal, up);

            SurfaceConstraintInfo newConstraint = constraint;
            newConstraint.Plane.Normal = math.normalize(newConstraint.Plane.Normal - verticalComponent * up);
            newConstraint.IsMaxSlope = true;

            float distance = newConstraint.Plane.Distance;

            // Calculate distance to the original plane along the new normal.
            // Clamp the new distance to 2x the old distance to avoid penetration recovery explosions.
            newConstraint.Plane.Distance =
                distance / math.max(math.dot(newConstraint.Plane.Normal, constraint.Plane.Normal), 0.5f);

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
            int hitRigidBodyIndex, ColliderKey hitColliderKey, float3 hitPosition, float3 hitSurfaceNormal,
            float hitDistance,
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

        private static void CalculateAndStoreDeferredImpulsesAndCollisionEvents(
            StepInput stepInput, bool affectBodies, float characterMass,
            float3 linearVelocity, NativeList<SurfaceConstraintInfo> constraints,
            ref NativeStream.Writer deferredImpulseWriter,
            NativeList<StatefulCollisionEvent> collisionEvents)
        {
            PhysicsWorld world = stepInput.PhysicsWorldSingleton.PhysicsWorld;
            for (int i = 0; i < constraints.Length; i++)
            {
                SurfaceConstraintInfo constraint = constraints[i];
                int rigidBodyIndex = constraint.RigidBodyIndex;

                float3 impulse = float3.zero;

                if (rigidBodyIndex < 0)
                {
                    continue;
                }

                // Skip static bodies if needed to calculate impulse
                if (affectBodies && (rigidBodyIndex < world.NumDynamicBodies))
                {
                    RigidBody body = world.Bodies[rigidBodyIndex];

                    float3 pointRelVel = world.GetLinearVelocity(rigidBodyIndex, constraint.HitPosition);
                    pointRelVel -= linearVelocity;

                    float projectedVelocity = math.dot(pointRelVel, constraint.Plane.Normal);

                    // Required velocity change
                    float deltaVelocity = -projectedVelocity * stepInput.Damping;

                    float distance = constraint.Plane.Distance;
                    if (distance < 0.0f)
                    {
                        deltaVelocity += (distance / stepInput.DeltaTime) * stepInput.Tau;
                    }

                    // Calculate impulse
                    MotionVelocity mv = world.MotionVelocities[rigidBodyIndex];
                    if (deltaVelocity < 0.0f)
                    {
                        // Impulse magnitude
                        float impulseMagnitude = 0.0f;
                        {
                            float objectMassInv = GetInvMassAtPoint(constraint.HitPosition, constraint.Plane.Normal,
                                body, mv);
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
                        new DeferredCharacterImpulse()
                        {
                            Entity = body.Entity,
                            Impulse = impulse,
                            Point = constraint.HitPosition
                        });
                }

                if (collisionEvents.IsCreated && constraint.Touched && !constraint.IsMaxSlope)
                {
                    var collisionEvent = new StatefulCollisionEvent()
                    {
                        EntityA = world.Bodies[stepInput.RigidBodyIndex].Entity,
                        EntityB = world.Bodies[rigidBodyIndex].Entity,
                        BodyIndexA = stepInput.RigidBodyIndex,
                        BodyIndexB = rigidBodyIndex,
                        ColliderKeyA = ColliderKey.Empty,
                        ColliderKeyB = constraint.ColliderKey,
                        Normal = constraint.Plane.Normal
                    };
                    collisionEvent.CollisionDetails = new StatefulCollisionEvent.Details(
                        1, math.dot(impulse, collisionEvent.Normal), constraint.HitPosition);

                    // check if collision event exists for the same bodyID and colliderKey
                    // although this is a nested for, number of solved constraints shouldn't be high
                    // if the same constraint (same entities, rigidbody indices and collider keys)
                    // is solved in multiple solver iterations, pick the one from latest iteration
                    bool newEvent = true;
                    for (int j = 0; j < collisionEvents.Length; j++)
                    {
                        if (collisionEvents[j].CompareTo(collisionEvent) == 0)
                        {
                            collisionEvents[j] = collisionEvent;
                            newEvent = false;
                            break;
                        }
                    }

                    if (newEvent)
                    {
                        collisionEvents.Add(collisionEvent);
                    }
                }
            }
        }

        private static void UpdateTriggersSeen<T>(StepInput stepInput, NativeList<T> triggerHits,
            NativeList<StatefulTriggerEvent> currentFrameTriggerEvents, float maxFraction)
            where T : unmanaged, IQueryResult
        {
            var world = stepInput.PhysicsWorldSingleton.PhysicsWorld;
            for (int i = 0; i < triggerHits.Length; i++)
            {
                var hit = triggerHits[i];

                if (hit.Fraction > maxFraction)
                {
                    continue;
                }

                var found = false;
                for (int j = 0; j < currentFrameTriggerEvents.Length; j++)
                {
                    var triggerEvent = currentFrameTriggerEvents[j];
                    if ((triggerEvent.EntityB == hit.Entity) &&
                        (triggerEvent.ColliderKeyB.Value == hit.ColliderKey.Value))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    currentFrameTriggerEvents.Add(new StatefulTriggerEvent()
                    {
                        EntityA = world.Bodies[stepInput.RigidBodyIndex].Entity,
                        EntityB = hit.Entity,
                        BodyIndexA = stepInput.RigidBodyIndex,
                        BodyIndexB = hit.RigidBodyIndex,
                        ColliderKeyA = ColliderKey.Empty,
                        ColliderKeyB = hit.ColliderKey
                    });
                }
            }
        }

        static float GetInvMassAtPoint(float3 point, float3 normal, RigidBody body, MotionVelocity mv)
        {
            var massCenter =
                math.transform(body.WorldFromBody, body.Collider.Value.MassProperties.MassDistribution.Transform.pos);
            float3 arm = point - massCenter;
            float3 jacAng = math.cross(arm, normal);
            float3 armC = jacAng * mv.InverseInertia;

            float objectMassInv = math.dot(armC, jacAng);
            objectMassInv += mv.InverseMass;

            return objectMassInv;
        }
    }
}

using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using UnityEngine.Assertions;

public struct SurfaceConstraintInfo
{
    // Info of interest for the character
    public Plane Plane;
    public float3 Velocity;

    // Hit body info
    public int RigidBodyIndex;
    public ColliderKey ColliderKey;
    public float3 HitPosition;

    // Internal state
    public int Priority;
    public bool Touched;
}
public struct DeferredCharacterControllerImpulse
{
    public Entity Entity;
    public float3 Impulse;
    public float3 Point;
}

public static class SimplexSolver
{
    public const float c_SimplexSolverEpsilon = 0.0001f;

    public struct SupportPlane
    {
        public int Index;
        public SurfaceConstraintInfo SurfaceConstraintInfo;
    }

    public static unsafe void Solve(PhysicsWorld world, float deltaTime, float3 up, int numConstraints,
        ref NativeArray<SurfaceConstraintInfo> constraints, ref float3 position, ref float3 velocity, out float integratedTime)
    {
        // List of planes to solve against (up to 4)
        byte* supportPlanesMemory = stackalloc byte[4 * sizeof(SupportPlane)];
        SupportPlane* supportPlanes = (SupportPlane*)supportPlanesMemory;
        int numSupportPlanes = 0;

        float remainingTime = deltaTime;
        float currentTime = 0.0f;

        // petarm.todo: introduce minDeltaTime after which solver breaks

        while (remainingTime > 0.0f)
        {
            int hitIndex = -1;
            float minCollisionTime = remainingTime;

            // Iterate over constraints and solve them
            for (int i = 0; i < numConstraints; i++)
            {
                if (constraints[i].Touched) continue;
                if (numSupportPlanes >= 1 && supportPlanes[0].Index == i) continue;
                if (numSupportPlanes >= 2 && supportPlanes[1].Index == i) continue;
                if (numSupportPlanes >= 3 && supportPlanes[2].Index == i) continue;

                SurfaceConstraintInfo constraint = constraints[i];

                float3 relVel = velocity - constraint.Velocity;
                float relProjVel = -math.dot(relVel, constraint.Plane.Normal);
                if (relProjVel <= 0.0f)
                {
                    continue;
                }

                // Clamp distance to 0, since penetration is handled by constraint.Velocity already
                float distance = math.max(constraint.Plane.Distance, 0.0f);
                if (distance <= minCollisionTime * relProjVel)
                {
                    minCollisionTime = distance / relProjVel;
                    hitIndex = i;
                }
            }

            // Integrate
            {
                float minCollisionTimeClamped = math.max(minCollisionTime, 0.0f);
                currentTime += minCollisionTimeClamped;
                remainingTime -= minCollisionTimeClamped;
                position += minCollisionTime * velocity;
            }

            if (hitIndex < 0)
            {
                break;
            }

            // Mark constraint as touched
            {
                var constraint = constraints[hitIndex];
                constraint.Touched = true;
                constraints[hitIndex] = constraint;
            }

            //  Add the hit to the current list of active planes
            {
                SupportPlane supportPlane = new SupportPlane()
                {
                    Index = hitIndex,
                    SurfaceConstraintInfo = constraints[hitIndex]
                };
                supportPlanes[numSupportPlanes] = supportPlane;
                numSupportPlanes++;
            }

            // Solve support planes
            ExamineActivePlanes(up, supportPlanes, ref numSupportPlanes, ref velocity);

            // Can't handle more than 4 support planes
            if (numSupportPlanes == 4)
            {
                break;
            }
        }

        integratedTime = currentTime;
    }

    private static unsafe  void ExamineActivePlanes(float3 up, SupportPlane* supportPlanes, ref int numSupportPlanes, ref float3 velocity)
    {
        switch (numSupportPlanes)
        {
            case 1:
                {
                    Solve1d(supportPlanes[0], ref velocity);
                    return;
                }
            case 2:
                {
                    // Test whether we need plane 0 at all
                    float3 tempVelocity = velocity;
                    Solve1d(supportPlanes[1], ref tempVelocity);

                    bool plane0Used = Test1d(supportPlanes[0], tempVelocity);
                    if (!plane0Used)
                    {
                        // Compact the buffer and reduce size
                        supportPlanes[0] = supportPlanes[1];
                        numSupportPlanes = 1;

                        // Write back the result
                        velocity = tempVelocity;
                    }
                    else
                    {
                        Solve2d(up, supportPlanes[0], supportPlanes[1], ref velocity);
                    }

                    return;
                }
            case 3:
                {
                    // Try to drop both planes
                    float3 tempVelocity = velocity;
                    Solve1d(supportPlanes[2], ref tempVelocity);

                    bool plane0Used = Test1d(supportPlanes[0], tempVelocity);
                    if (!plane0Used)
                    {
                        bool plane1Used = Test1d(supportPlanes[1], tempVelocity);
                        if (!plane1Used)
                        {
                            // Compact the buffer and reduce size
                            supportPlanes[0] = supportPlanes[2];
                            numSupportPlanes = 1;
                            goto case 1;
                        }
                    }

                    // Try to drop plane 0 or 1
                    for (int testPlane = 0; testPlane < 2; testPlane++)
                    {
                        tempVelocity = velocity;
                        Solve2d(up, supportPlanes[testPlane], supportPlanes[2], ref tempVelocity);

                        bool planeUsed = Test1d(supportPlanes[1 - testPlane], tempVelocity);
                        if (!planeUsed)
                        {
                            supportPlanes[0] = supportPlanes[testPlane];
                            supportPlanes[1] = supportPlanes[2];
                            numSupportPlanes--;
                            goto case 2;
                        }
                    }

                    // Try solve all three
                    Solve3d(up, supportPlanes[0], supportPlanes[1], supportPlanes[2], ref velocity);

                    return;
                }
            case 4:
                {
                    for (int i = 0; i < 3; i++)
                    {
                        float3 tempVelocity = velocity;
                        Solve3d(up, supportPlanes[(i + 1) % 3], supportPlanes[(i + 2) % 3], supportPlanes[3], ref tempVelocity);
                        bool planeUsed = Test1d(supportPlanes[i], tempVelocity);
                        if (!planeUsed)
                        {
                            supportPlanes[i] = supportPlanes[2];
                            supportPlanes[2] = supportPlanes[3];
                            numSupportPlanes = 3;
                            goto case 3;
                        }
                    }

                    // Nothing can be dropped so we've failed to solve,
                    // now we do all 3d combinations
                    float3 tempVel = velocity;
                    SupportPlane sp0 = supportPlanes[0];
                    SupportPlane sp1 = supportPlanes[1];
                    SupportPlane sp2 = supportPlanes[2];
                    SupportPlane sp3 = supportPlanes[3];
                    Solve3d(up, sp0, sp1, sp2, ref tempVel);
                    Solve3d(up, sp0, sp1, sp3, ref tempVel);
                    Solve3d(up, sp0, sp2, sp3, ref tempVel);
                    Solve3d(up, sp1, sp2, sp3, ref tempVel);

                    velocity = tempVel;

                    return;
                }
            default:
                {
                    // Can't have more than 4 and less than 1 plane
                    Assert.IsTrue(false);
                    break;
                }
        }
    }

    private static void Solve1d(SupportPlane supportPlane, ref float3 velocity)
    {
        SurfaceConstraintInfo constraint = supportPlane.SurfaceConstraintInfo;
        float3 groundVelocity = constraint.Velocity;
        float3 relVel = velocity - groundVelocity;
        float planeVel = math.dot(relVel, constraint.Plane.Normal);
        relVel -= planeVel * constraint.Plane.Normal;

        velocity = relVel + groundVelocity;
    }

    private static bool Test1d(SupportPlane supportPlane, float3 velocity)
    {
        SurfaceConstraintInfo constraint = supportPlane.SurfaceConstraintInfo;
        float3 relVel = velocity - constraint.Velocity;
        float planeVel = math.dot(relVel, constraint.Plane.Normal);
        return planeVel < -c_SimplexSolverEpsilon;
    }

    private static void Solve2d(float3 up, SupportPlane supportPlane0, SupportPlane supportPlane1, ref float3 velocity)
    {
        SurfaceConstraintInfo constraint0 = supportPlane0.SurfaceConstraintInfo;
        SurfaceConstraintInfo constraint1 = supportPlane1.SurfaceConstraintInfo;

        float3 plane0 = constraint0.Plane.Normal;
        float3 plane1 = constraint1.Plane.Normal;

        // Calculate the free axis
        float3 axis = math.cross(plane0, plane1);
        float axisLen2 = math.lengthsq(axis);

        // Check for parallel planes
        if (axisLen2 < c_SimplexSolverEpsilon)
        {
            // Do the planes sequentially
            Sort2d(ref supportPlane0, ref supportPlane1);
            Solve1d(supportPlane1, ref velocity);
            Solve1d(supportPlane0, ref velocity);

            return;
        }

        float invAxisLen = math.rsqrt(axisLen2);
        axis *= invAxisLen;

        // Calculate the velocity of the free axis
        float3 axisVel;
        {
            float4x4 m = new float4x4();
            float3 r0 = math.cross(plane0, plane1);
            float3 r1 = math.cross(plane1, axis);
            float3 r2 = math.cross(axis, plane0);
            m.c0 = new float4(r0, 0.0f);
            m.c1 = new float4(r1, 0.0f);
            m.c2 = new float4(r2, 0.0f);
            m.c3 = new float4(0.0f, 0.0f, 0.0f, 1.0f);

            float3 sVel = constraint0.Velocity + constraint1.Velocity;
            float3 t = new float3(
                math.dot(axis, sVel) * 0.5f,
                math.dot(plane0, constraint0.Velocity),
                math.dot(plane1, constraint1.Velocity));

            axisVel = math.rotate(m, t);
            axisVel *= invAxisLen;
        }

        float3 groundVelocity = axisVel;
        float3 relVel = velocity - groundVelocity;

        float vel2 = math.lengthsq(relVel);
        float axisVert = math.dot(up, axis);
        float axisProjVel = math.dot(relVel, axis);

        velocity = groundVelocity + axis * axisProjVel;
    }

    private static void Solve3d(float3 up, SupportPlane supportPlane0, SupportPlane supportPlane1, SupportPlane supportPlane2, ref float3 velocity)
    {
        SurfaceConstraintInfo constraint0 = supportPlane0.SurfaceConstraintInfo;
        SurfaceConstraintInfo constraint1 = supportPlane1.SurfaceConstraintInfo;
        SurfaceConstraintInfo constraint2 = supportPlane2.SurfaceConstraintInfo;

        float3 plane0 = constraint0.Plane.Normal;
        float3 plane1 = constraint1.Plane.Normal;
        float3 plane2 = constraint2.Plane.Normal;

        float4x4 m = new float4x4();
        float3 r0 = math.cross(plane1, plane2);
        float3 r1 = math.cross(plane2, plane0);
        float3 r2 = math.cross(plane0, plane1);
        m.c0 = new float4(r0, 0.0f);
        m.c1 = new float4(r1, 0.0f);
        m.c2 = new float4(r2, 0.0f);
        m.c3 = new float4(0.0f, 0.0f, 0.0f, 1.0f);

        float det = m.c0.x * m.c1.y * m.c2.z;
        float tst = math.abs(det);
        if (tst < c_SimplexSolverEpsilon)
        {
            Sort3d(ref supportPlane0, ref supportPlane1, ref supportPlane2);
            Solve2d(up, supportPlane1, supportPlane2, ref velocity);
            Solve2d(up, supportPlane0, supportPlane2, ref velocity);
            Solve2d(up, supportPlane0, supportPlane1, ref velocity);

            return;
        }

        float3 sVel = constraint0.Velocity + constraint1.Velocity;
        float3 t = new float3(
            math.dot(plane0, constraint0.Velocity),
            math.dot(plane1, constraint1.Velocity),
            math.dot(plane2, constraint2.Velocity));

        float3 pointVel = math.rotate(m, t);
        pointVel /= det;

        velocity = pointVel;
    }

    private static void Sort2d(ref SupportPlane plane0, ref SupportPlane plane1)
    {
        int priority0 = plane0.SurfaceConstraintInfo.Priority;
        int priority1 = plane1.SurfaceConstraintInfo.Priority;
        if (priority0 > priority1)
        {
            SwapPlanes(ref plane0, ref plane1);
        }
    }

    private static void Sort3d(ref SupportPlane plane0, ref SupportPlane plane1, ref SupportPlane plane2)
    {
        int priority0 = plane0.SurfaceConstraintInfo.Priority;
        int priority1 = plane1.SurfaceConstraintInfo.Priority;
        int priority2 = plane2.SurfaceConstraintInfo.Priority;
        if (priority0 <= priority1)
        {
            if (priority1 <= priority2)
            {
                // 0, 1, 2
            }
            else if (priority0 <= priority2)
            {
                // 0, 2, 1
                SwapPlanes(ref plane1, ref plane2);
            }
            else
            {
                // 1, 2, 0
                SwapPlanes(ref plane0, ref plane1);
                SwapPlanes(ref plane0, ref plane2);
            }
        }
        else
        {
            if (priority2 < priority1)
            {
                // 2, 1, 0
                SwapPlanes(ref plane0, ref plane2);
            }
            else if (priority2 > priority0)
            {
                // 1, 0, 2
                SwapPlanes(ref plane0, ref plane1);
            }
            else
            {
                // 2, 0, 1
                SwapPlanes(ref plane0, ref plane1);
                SwapPlanes(ref plane1, ref plane2);
            }
        }
    }

    private static void SwapPlanes(ref SupportPlane plane0, ref SupportPlane plane1)
    {
        var temp = plane0;
        plane0 = plane1;
        plane1 = temp;
    }
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

        #region

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
        float characterMass, float tau, float damping, bool affectBodies, Collider* collider,
        ref NativeArray<DistanceHit> distanceHits, ref NativeArray<ColliderCastHit> castHits, ref NativeArray<SurfaceConstraintInfo> constraints,
        ref RigidTransform transform, ref float3 linearVelocity, ref BlockStream.Writer deferredImpulseWriter)
    {
        float remainingTime = deltaTime;
        float3 lastDisplacement = linearVelocity * remainingTime;

        float3 newPosition = transform.pos;
        quaternion orientation = transform.rot;
        float3 newVelocity = linearVelocity;

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
                    constraints[numConstraints++] = constraint;
                }
            }

            // Then do a collider cast
            {
                float3 displacement = lastDisplacement - up * timeEpsilon;
                float3 endPosition = newPosition + displacement;
                MaxHitsCollector<ColliderCastHit> collector = new MaxHitsCollector<ColliderCastHit>(1.0f, ref castHits);
                ColliderCastInput input = new ColliderCastInput()
                {
                    Collider = collider,
                    Orientation = orientation,
                    Position = newPosition,
                    Direction = displacement
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
                        constraints[numConstraints++] = constraint;
                    }
                }
            }

            // petarm.todo: Add max slope plane to avoid climbing the not allowed slopes

            // Solve
            float3 prevVelocity = newVelocity;
            SimplexSolver.Solve(world, deltaTime, up, numConstraints, ref constraints, ref newPosition, ref newVelocity, out float integratedTime);

            remainingTime -= integratedTime;
            lastDisplacement = newVelocity * remainingTime;

            // Apply impulses to hit bodies
            if (affectBodies)
            {
                ResolveContacts(world, deltaTime, gravity, tau, damping, characterMass, prevVelocity, numConstraints, ref constraints, ref deferredImpulseWriter);
            }
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

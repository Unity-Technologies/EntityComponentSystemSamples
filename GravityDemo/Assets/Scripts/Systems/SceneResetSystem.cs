using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class SceneResetSystem : ComponentSystem {

    struct AsteroidData
    {
        public readonly int Length;
        public ComponentDataArray<Position> positions;
        public ComponentDataArray<Asteroid> asteroids;
        public ComponentDataArray<Velocity> velocities;
    }

    struct StarData
    {
        public readonly int Length;
        public ComponentDataArray<Star> stars;
        public GameObjectArray starsGO;
    }

    [Inject] private AsteroidData asteroids;
    [Inject] private StarData stars;
    
    float3 randomVector => new float3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));

    public void Ball1()
    {
        for (int i = 0; i < asteroids.Length; i++)
        {
            var starPosition = stars.starsGO[i % stars.Length].transform.position;
            
            var asteroidPosition = asteroids.positions[i];
            var asteroidVelocity = asteroids.velocities[i];
            
            var pos = starPosition + (Quaternion.AngleAxis(Random.Range(0f, 360), randomVector) * Vector3.forward * 100);
            asteroidPosition.Value = pos;
            asteroidVelocity.Value = math.normalize(math.cross(pos - starPosition, randomVector)) * Random.Range(2f, 3f);
            
            asteroids.positions[i] = asteroidPosition;
            asteroids.velocities[i] = asteroidVelocity;
        }
    }
    
    public void Ball2()
    {
        for (int i = 0; i < asteroids.Length; i++)
        {
            var starPosition = stars.starsGO[i % stars.Length].transform.position;
            
            var asteroidPosition = asteroids.positions[i];
            var asteroidVelocity = asteroids.velocities[i];
            
            var pos = starPosition + (Quaternion.AngleAxis(Random.Range(0, 360), new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f))) * Vector3.forward * 100);
            asteroidPosition.Value = pos;
            asteroidVelocity.Value = math.normalize(math.cross(pos - starPosition, Vector3.up)) * 4;
            
            asteroids.positions[i] = asteroidPosition;
            asteroids.velocities[i] = asteroidVelocity;
        }
    }
    
    public void Ball3()
    {
        for (int i = 0; i < asteroids.Length; i++)
        {
            var starPosition = stars.starsGO[i % stars.Length].transform.position;
            
            var asteroidPosition = asteroids.positions[i];
            var asteroidVelocity = asteroids.velocities[i];
            
            var pos = starPosition + (Quaternion.AngleAxis(Random.Range(0f, 360f), randomVector) * Vector3.down * Random.Range(100f, 105f));
            asteroidPosition.Value = pos;
            asteroidVelocity.Value = math.normalize(math.cross(pos - starPosition, randomVector)) * Random.Range(0.9f, 1f);
            
            asteroids.positions[i] = asteroidPosition;
            asteroids.velocities[i] = asteroidVelocity;
        }
    }
    
    public void Ball4()
    {

        float a = 0;
        var step = 360f / SimulationBootstrap.settings.SimSize;
        for (int i = 0; i < asteroids.Length; i++)
        {
            var starPosition = stars.starsGO[i % stars.Length].transform.position;
            
            var asteroidPosition = asteroids.positions[i];
            var asteroidVelocity = asteroids.velocities[i];
            
            var d = (Quaternion.AngleAxis(a += step, Vector3.up) * Vector3.forward * Random.Range(55f, 120f));
            var pos = starPosition + d;
            
            asteroidPosition.Value = pos;
            asteroidVelocity.Value = math.cross(math.normalize(d), Vector3.up) * 3.5f;

            asteroids.positions[i] = asteroidPosition;
            asteroids.velocities[i] = asteroidVelocity;
        }
    }
    
    public void Disk()
    {
        for (int i = 0; i < asteroids.Length; i++)
        {
            var starPosition = stars.starsGO[i % stars.Length].transform.position;
            
            var asteroidPosition = asteroids.positions[i];
            var asteroidVelocity = asteroids.velocities[i];
            
            asteroidPosition.Value = starPosition + (Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.down) * Vector3.forward * Random.Range(0, 155f));
            asteroidPosition.Value += new float3(0, 180, 0);
            asteroidVelocity.Value = Vector3.down * 3;
            
            asteroids.positions[i] = asteroidPosition;
            asteroids.velocities[i] = asteroidVelocity;
        }
    }
    
    public void Disk2()
    {
        for (int i = 0; i < asteroids.Length; i++)
        {
            var starPosition = stars.starsGO[i % stars.Length].transform.position;
            
            var asteroidPosition = asteroids.positions[i];
            var asteroidVelocity = asteroids.velocities[i];
            
            var pos = starPosition + (Quaternion.AngleAxis(Random.Range(0f, 360f), Vector3.down) * Vector3.forward * Random.Range(0, 150f));
            pos += new Vector3(0, 180, 0);
            asteroidVelocity.Value = (new float3(Vector3.down) * 3) + math.normalize(math.cross(pos - starPosition, Vector3.down));
            asteroidPosition.Value = pos;
                        
            asteroids.positions[i] = asteroidPosition;
            asteroids.velocities[i] = asteroidVelocity;
        }
    }

    public void Spiral()
    {
        int numberArms = 20;

        float galaxyRadius = 300f;
        int spread = 50;
        float fAngularSpread = spread / numberArms;
        float fArmAngle = (360 / numberArms);      
          
        for (int i = 0; i < asteroids.Length; i++)
        {
            float3 starPosition = stars.starsGO[i % stars.Length].transform.position;
            
            var asteroidPosition = asteroids.positions[i];
            var asteroidVelocity = asteroids.velocities[i];
            
            float fR = fHatRandom(galaxyRadius);
            float fQ = fLineRandom(fAngularSpread);
            float fK = 1;

            float fA = (i % numberArms) * fArmAngle;
            float fX = fR * Mathf.Cos(Mathf.Deg2Rad * (fA + fR * fK + fQ));
            float fY = fR * Mathf.Sin(Mathf.Deg2Rad * (fA + fR * fK + fQ));
            var pos = new float3(fX, 100, fY);

            asteroidVelocity.Value = new float3(0);
            asteroidPosition.Value = pos + new float3(500);

            asteroids.positions[i] = asteroidPosition;
            asteroids.velocities[i] = asteroidVelocity;
        }
   
    }

    float fHatRandom(float fRange)
    {
        float fArea = 4 * Mathf.Atan(6.0f);
        float fP = fArea * Random.value;
        return Mathf.Tan(fP / 4) * fRange / 6.0f;
    }
    
    float fLineRandom(float fRange)
    {
        float fArea = fRange * fRange / 2;
        float fP = fArea * Random.value;
        return fRange - Mathf.Sqrt(fRange * fRange - 2 * fP);
    }

    void InstantiateNewEntities(int count)
    {
        var entityManager = World.Active.GetOrCreateManager<EntityManager>();

        var asteroidTransform = SimulationBootstrap.settings.asteroidPrefab.transform;
        var cameraTransform = Camera.main.transform;

        for (var i = 0; i < count; ++i)
        {
            var entity = entityManager.Instantiate(SimulationBootstrap.settings.asteroidPrefab);

            float3 position = cameraTransform.position + new Vector3(10,0,10) + (Random.insideUnitSphere * 5);
            entityManager.SetComponentData(entity, new Position { Value = position });

            float3 velocity = (cameraTransform.forward) * (SimulationBootstrap.settings.AsteroidSpeed + Random.Range(-SimulationBootstrap.settings.StartSpeedRandomness, SimulationBootstrap.settings.StartSpeedRandomness));
            entityManager.SetComponentData(entity, new Velocity { Value = velocity });

            float mass = Random.Range(-SimulationBootstrap.settings.MassRandomness, SimulationBootstrap.settings.MassRandomness) + (asteroidTransform.localScale.x * asteroidTransform.localScale.x);
            entityManager.SetComponentData(entity, new Mass { Value = mass });

            entityManager.SetComponentData(entity, new Asteroid());
        }
    }
    
    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.Alpha1))
            Spiral();
        else if (Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.Alpha2))
            Ball1();
        else if (Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.Alpha3))
            Ball2();
        else if (Input.GetKeyDown(KeyCode.Keypad4) || Input.GetKeyDown(KeyCode.Alpha4))
            Ball3();
        else if (Input.GetKeyDown(KeyCode.Keypad5) || Input.GetKeyDown(KeyCode.Alpha5))
            Ball4();
        else if (Input.GetKeyDown(KeyCode.Keypad6) || Input.GetKeyDown(KeyCode.Alpha6))
            Disk();
        else if (Input.GetKeyDown(KeyCode.Keypad7) || Input.GetKeyDown(KeyCode.Alpha7))
            Disk2();
        else if (Input.GetKey(KeyCode.Space))
            InstantiateNewEntities(500);
    }
    
}

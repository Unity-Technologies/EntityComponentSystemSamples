using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Jobs;


public class SoftBody : MonoBehaviour
{
    public Vector3 spread;

    public float massRandomness;

    public float startSpeedRandomness;

    public float minSize = 1;

    public float maxSize = 3;

    public float simulationSpeed;

    public float gravityConstant = 9;

    public ParticleSystem _particleSystem;

    NativeArray<SoftBodyParticleData> particlesData;


    NativeArray<ParticleSystem.Particle> particlesNative;

    ParticleSystem.Particle[] particles;

   
    Vector3 randomVector
    {
        get { return new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f)); }
    }


  
    private void Start()
    {
        int size = 128;


        particlesData = new NativeArray<SoftBodyParticleData>(size, Allocator.Persistent);
 
        for (int i = 0; i < size; i++)
        {
          
            var d = new SoftBodyParticleData();
            d.size = Random.Range(.5f, 1f);
            d.mass = d.size * 5;
            d.position = new Vector3(0, 5, 0) + new Vector3(0, i, 0);
            particlesData[i] = d;
          
        }

        particles = new ParticleSystem.Particle[particlesData.Length];
        particlesNative = new NativeArray<ParticleSystem.Particle>(particles, Allocator.Persistent);
    }

    private void OnDestroy()
    {

    }

    private void OnApplicationQuit()
    {
        particlesData.Dispose();
        particlesNative.Dispose();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
 
        var job = new SoftBodyJob();
        job.particles = particlesNative;
        job.partData = particlesData;

        job.dt = Time.deltaTime * simulationSpeed;

    
        var handle = job.Schedule();
        handle.Complete();

        job.particles.CopyTo(particles);
        _particleSystem.SetParticles(particles, particles.Length);

    }

}

[System.Serializable]
public struct SoftBodyParticleData
{
    public float mass;

    public Vector3 velocity, position;
    public float size;


    public Vector3 Process(float dt)
    {
       
        var v = Vector3.down * 9;
        var f = 9 * mass / v.sqrMagnitude;
        velocity += (v.normalized * f * dt) / mass;
        position += velocity * dt;
        if (position.y < size / 2)
        {
            position.y = size / 2;
            velocity.y = 0;
        }
               
       
      
        return position;
    }

    public bool IsColliding(SoftBodyParticleData other)
    {
       
        Vector3 s = position - other.position; // vector between the centers of each sphere
        Vector3 v = velocity -other.velocity; // relative velocity between spheres
        float r = size + other.size;
        float t = 0;
        float c = Vector3.Dot(s,s) - r * r; // if negative, they overlap
        if (c < 0.0f) // if true, they already overlap
        {
            t = .0f;
            return true;
        }

        float a = Vector3.Dot(v, v);

        float b = Vector3.Dot(v, s);
        if (b >= 0.0f)
            return false; // does not move towards each other

        float d = b * b - a * c;
        if (d < 0.0f)
            return false; // no real roots ... no collision

        t = (-b - Mathf.Sqrt(d)) / a;

        return true;

    }
    public void Resolve(SoftBodyParticleData coll)
    {
        float m1, m2, x1, x2;
        var x = (position - coll.position).normalized;
        Vector3 v1, v2, v1x, v2x, v1y, v2y;

        v1 = velocity;
        x1 = Vector3.Dot(x, v1);
        v1x = x * x1;
        v1y = v1 - v1x;
        m1 = mass;

        x = x * -1;
        v2 = coll.velocity;
        x2 = Vector3.Dot(x, v2);
        v2x = x * x2;
        v2y = v2 - v2x;
        m2 = coll.mass;

        velocity = v1x * (m1 - m2) / (m1 + m2) + v2x * (2 * m2) / (m1 + m2) + v1y;
        coll.velocity = v1x * (2 * m1) / (m1 + m2) + v2x * (m2 - m1) / (m1 + m2) + v2y;
    }
}



public struct SoftBodyJob : IJob
{
    public NativeArray<ParticleSystem.Particle> particles;
    public NativeArray<SoftBodyParticleData> partData;
    public float dt;

    public void Execute()
    {
        var l = partData.Length;
        for (int j = 0; j <l ; j++)
        {
            var part = particles[j];
            var data = partData[j];
         
            part.position = data.Process(dt);
            for (int i = 0; i < l; i++)
            {
                if (i != j)
                {
                    //   if(data.IsColliding( partData[i]))
                    data.Resolve(partData[i]);
                }

            }

            part.velocity = Vector3.zero;
            part.remainingLifetime = 5;
            part.startSize = data.size;
            
            
            partData[j] = data;
            particles[j] = part;
        }
       
        
    }
}

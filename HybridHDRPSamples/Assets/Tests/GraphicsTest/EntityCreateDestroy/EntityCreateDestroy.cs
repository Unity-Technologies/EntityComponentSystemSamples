#if ENABLE_HYBRID_RENDERER_V2

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Entities;

public class EntityCreateDestroy : MonoBehaviour
{
    public GameObject prefab;
    public int countX = 100;
    public int countY = 100;
    public int countZ = 10;
    public float addFrequency = 10.0f;

    private GameObjectConversionSettings _settings;
    private Entity _prefabEntity;
    private EntityManager _entityManager;
    private int _direction = 1;
    private int _currentPosition = 0;
    private List<float3> _positions = new List<float3>();
    private List<Entity> _entities = new List<Entity>();
    private int _frameCounter = 0;
    private List<Action<int>> _addComponent = new List<Action<int>>();
    private List<Action<int>> _removeComponent = new List<Action<int>>();

    // Start is called before the first frame update
    void Start()
    {
        // Create entity prefab from the game object hierarchy once
        _settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, null);
        _prefabEntity = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, _settings);
        _entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        createLocations();
        createActions();
    }

    private void createLocations()
    {
        for (var z = 0; z < countZ; z++)
            for (var x = 0; x < countX; x++)
                for (var y = 0; y < countY; y++)
                    _positions.Add(transform.TransformPoint(new float3(x * 1.3F,
                        noise.cnoise(new float2(x, y) * 0.21F) * 2 + (z * 2.0F), y * 1.3F)));
    }

    private void createActions()
    {
        _addComponent.Add((int entityIndex) => {
            if (!_entityManager.HasComponent<TestComponentA>(_entities[entityIndex]))
                _entityManager.AddComponent<TestComponentA>(_entities[entityIndex]); });
        _addComponent.Add((int entityIndex) => {
            if (!_entityManager.HasComponent<TestComponentB>(_entities[entityIndex]))
                _entityManager.AddComponent<TestComponentB>(_entities[entityIndex]); });
        _addComponent.Add((int entityIndex) => {
            if (!_entityManager.HasComponent<TestComponentC>(_entities[entityIndex]))
                _entityManager.AddComponent<TestComponentC>(_entities[entityIndex]); });

        _removeComponent.Add((int entityIndex) => {
            if (_entityManager.HasComponent<TestComponentA>(_entities[entityIndex]))
                _entityManager.RemoveComponent<TestComponentA>(_entities[entityIndex]); });
        _removeComponent.Add((int entityIndex) => {
            if (_entityManager.HasComponent<TestComponentB>(_entities[entityIndex]))
                _entityManager.RemoveComponent<TestComponentB>(_entities[entityIndex]); });
        _removeComponent.Add((int entityIndex) => {
            if (_entityManager.HasComponent<TestComponentC>(_entities[entityIndex]))
                _entityManager.RemoveComponent<TestComponentC>(_entities[entityIndex]); });
    }

    private void IncreasePosition()
    {
        _currentPosition += _direction;
        if (_currentPosition > _positions.Count - 1 || _currentPosition < 0)
        {
            _currentPosition -= _direction;
            _direction *= -1;
        }
    }

    private void TestAddingRemovingEntities()
    {
        float freq = addFrequency;
        if (_direction < 0)
            freq /= 3;

        for (int i = 0; i < freq; ++i)
        {
            if (_direction > 0)
            {
                // adding
                var instance = _entityManager.Instantiate(_prefabEntity);
                _entityManager.SetComponentData(instance, new Translation {Value = _positions[_currentPosition]});
                _entities.Add(instance);
            }
            else
            {
                // removing
                _entityManager.DestroyEntity(_entities[_currentPosition]);
                _entities.RemoveAt(_currentPosition);
            }

            IncreasePosition();
        }
    }

    private void TestAddingRemovingComponents()
    {
        //return;

        if (_entities.Count == 0)
            return;

        if (_frameCounter % 3 == 0)
        {
            for (int i = 0; i < 100; ++i)
            {
                _addComponent[i % 3]((_frameCounter + i) % _entities.Count);
            }

            for (int i = 0; i < 100; ++i)
            {
                _removeComponent[i % 3]((_frameCounter + 100 + i) % _entities.Count);
            }
        }
        ++_frameCounter;
    }

    // Update is called once per frame
    void Update()
    {
        TestAddingRemovingEntities();
        TestAddingRemovingComponents();
    }

}

#endif // ENABLE_HYBRID_RENDERER_V2

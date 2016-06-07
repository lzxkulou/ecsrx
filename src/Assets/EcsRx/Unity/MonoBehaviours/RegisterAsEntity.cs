﻿using System;
using System.Collections.Generic;
using System.Linq;
using EcsRx.Components;
using EcsRx.Entities;
using EcsRx.Pools;
using EcsRx.Unity.Components;
using UnityEngine;
using Zenject;

namespace EcsRx.Unity.MonoBehaviours
{
    public class RegisterAsEntity : MonoBehaviour
    {
        [Inject]
        public IPoolManager PoolManager { get; private set; }

        public string PoolName;
        public List<string> StagedComponents = new List<string>();

        [Inject]
        public void RegisterEntity()
        {
            IPool poolToUse;

            if (string.IsNullOrEmpty(PoolName))
            { poolToUse = PoolManager.GetPool(); }
            else if (PoolManager.Pools.All(x => x.Name != PoolName))
            { poolToUse = PoolManager.CreatePool(PoolName); }
            else
            { poolToUse = PoolManager.GetPool(PoolName); }

            var createdEntity = poolToUse.CreateEntity();
            createdEntity.AddComponent(new ViewComponent { View = gameObject });
            SetupEntityBinding(createdEntity, poolToUse);
            SetupEntityComponents(createdEntity);
        }

        private void SetupEntityBinding(IEntity entity, IPool pool)
        {
            var entityBinding = gameObject.AddComponent<EntityBinding>();
            entityBinding.Entity = entity;
            entityBinding.Pool = pool;
            Destroy(this);
        }

        private void SetupEntityComponents(IEntity entity)
        {
            for (var i = 0; i < StagedComponents.Count(); i++)
            {
                var type = Type.GetType(StagedComponents[i]);
                var component = (IComponent)Activator.CreateInstance(type);
                entity.AddComponent(component);
            }
        }

        public IPool GetPool()
        {
            return PoolManager.GetPool(PoolName);
        }
    }
}
﻿using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;

namespace ET
{
    [Flags]
    public enum EntityStatus: byte
    {
        None = 0,
        IsFromPool = 1,
        IsRegister = 1 << 1,
        IsComponent = 1 << 2,
        IsCreated = 1 << 3,
        IsNew = 1 << 4,
    }

    public interface IScene
    {
        SceneType SceneType { get; set; }
    }

    public partial class Entity: DisposeObject
    {
#if UNITY_ET_VIEW && UNITY_EDITOR
        [BsonIgnore]
        private UnityEngine.GameObject viewGO;
        
        [StaticField]
        private static UnityEngine.Transform rootViewTransform;

        public static void SetRootView(UnityEngine.Transform root)
        {
            rootViewTransform = root;
        }
#endif
        
        [BsonIgnore]
        public long InstanceId { get; protected set; }

        protected Entity()
        {
        }

        [BsonIgnore]
        private EntityStatus status = EntityStatus.None;

        [BsonIgnore]
        private bool IsFromPool
        {
            get => (this.status & EntityStatus.IsFromPool) == EntityStatus.IsFromPool;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsFromPool;
                }
                else
                {
                    this.status &= ~EntityStatus.IsFromPool;
                }
            }
        }

        [BsonIgnore]
        protected bool IsRegister
        {
            get => (this.status & EntityStatus.IsRegister) == EntityStatus.IsRegister;
            set
            {
                if (this.IsRegister == value)
                {
                    return;
                }

                if (value)
                {
                    this.status |= EntityStatus.IsRegister;
                }
                else
                {
                    this.status &= ~EntityStatus.IsRegister;
                }

                if (value)
                {
                    this.RegisterSystem();
                }

#if UNITY_ET_VIEW && UNITY_EDITOR
                if (value)
                {
                    this.viewGO = new UnityEngine.GameObject(this.ViewName);
                    this.viewGO.AddComponent<ComponentView>().Component = this;
                    if (rootViewTransform == null)
                    {
                        rootViewTransform = UnityEngine.GameObject.Find("ET").transform;
                        if (rootViewTransform == null)
                        {
                            rootViewTransform = new UnityEngine.GameObject("ET").transform;
                            UnityEngine.Object.DontDestroyOnLoad(rootViewTransform);
                        }
                    }
                    this.viewGO.transform.SetParent(this.Parent == null? rootViewTransform : this.Parent.viewGO.transform);
                }
                else
                {
                    UnityEngine.Object.Destroy(this.viewGO);
                }
#endif
            }
        }

        protected virtual void RegisterSystem()
        {
            EntitySystemSingleton.Instance.RegisterSystem(this);
        }

        protected virtual string ViewName
        {
            get
            {
                return this.GetType().FullName;
            }
        }

        [BsonIgnore]
        protected bool IsComponent
        {
            get => (this.status & EntityStatus.IsComponent) == EntityStatus.IsComponent;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsComponent;
                }
                else
                {
                    this.status &= ~EntityStatus.IsComponent;
                }
            }
        }

        [BsonIgnore]
        protected bool IsCreated
        {
            get => (this.status & EntityStatus.IsCreated) == EntityStatus.IsCreated;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsCreated;
                }
                else
                {
                    this.status &= ~EntityStatus.IsCreated;
                }
            }
        }

        [BsonIgnore]
        protected bool IsNew
        {
            get => (this.status & EntityStatus.IsNew) == EntityStatus.IsNew;
            set
            {
                if (value)
                {
                    this.status |= EntityStatus.IsNew;
                }
                else
                {
                    this.status &= ~EntityStatus.IsNew;
                }
            }
        }

        [BsonIgnore]
        public bool IsDisposed => this.InstanceId == 0;

        [BsonIgnore]
        private Entity parent;

        // 可以改变parent，但是不能设置为null
        [BsonIgnore]
        public Entity Parent
        {
            get => this.parent;
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {this.GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {this.GetType().FullName}");
                }

                // 严格限制parent必须要有domain,也就是说parent必须在数据树上面
                if (value.Domain == null)
                {
                    throw new Exception($"cant set parent because parent domain is null: {this.GetType().FullName} {value.GetType().FullName}");
                }

                if (this.parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (this.parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                        return;
                    }

                    this.parent.RemoveFromChildren(this);
                }

                this.parent = value;
                this.IsComponent = false;
                this.parent.AddToChildren(this);

                this.Domain = this is IScene? this as IScene : this.parent.domain;

#if UNITY_ET_VIEW && UNITY_EDITOR
                this.viewGO.GetComponent<ComponentView>().Component = this;
                this.viewGO.transform.SetParent(this.Parent == null ?
                        rootViewTransform : this.Parent.viewGO.transform);
                foreach (var child in this.Children.Values)
                {
                    child.viewGO.transform.SetParent(this.viewGO.transform);
                }
                foreach (var comp in this.Components.Values)
                {
                    comp.viewGO.transform.SetParent(this.viewGO.transform);
                }
#endif
            }
        }

        // 该方法只能在AddComponent中调用，其他人不允许调用
        [BsonIgnore]
        private Entity ComponentParent
        {
            set
            {
                if (value == null)
                {
                    throw new Exception($"cant set parent null: {this.GetType().FullName}");
                }

                if (value == this)
                {
                    throw new Exception($"cant set parent self: {this.GetType().FullName}");
                }

                // 严格限制parent必须要有domain,也就是说parent必须在数据树上面
                if (value.Domain == null)
                {
                    throw new Exception($"cant set parent because parent domain is null: {this.GetType().FullName} {value.GetType().FullName}");
                }

                if (this.parent != null) // 之前有parent
                {
                    // parent相同，不设置
                    if (this.parent == value)
                    {
                        Log.Error($"重复设置了Parent: {this.GetType().FullName} parent: {this.parent.GetType().FullName}");
                        return;
                    }

                    this.parent.RemoveFromComponents(this);
                }

                this.parent = value;
                this.IsComponent = true;
                this.parent.AddToComponents(this);
                this.Domain = this is IScene? this as IScene : this.parent.domain;
            }
        }

        public T GetParent<T>() where T : Entity
        {
            return this.Parent as T;
        }

        [BsonIgnoreIfDefault]
        [BsonDefaultValue(0L)]
        [BsonElement]
        [BsonId]
        public long Id { get; protected set; }

        [BsonIgnore]
        protected IScene domain;

        [BsonIgnore]
        public IScene Domain
        {
            get
            {
                return this.domain;
            }
            protected set
            {
                if (value == null)
                {
                    throw new Exception($"domain cant set null: {this.GetType().FullName}");
                }

                if (this.domain == value)
                {
                    return;
                }

                IScene preDomain = this.domain;
                this.domain = value;

                if (preDomain == null)
                {
                    if (this.InstanceId == 0)
                    {
                        this.InstanceId = IdGenerater.Instance.GenerateInstanceId();
                    }

                    this.IsRegister = true;

                    // 反序列化出来的需要设置父子关系
                    if (this.componentsDB != null)
                    {
                        foreach (Entity component in this.componentsDB)
                        {
                            component.IsComponent = true;
                            this.Components.Add(component.GetType().FullName, component);
                            component.parent = this;
                        }
                    }

                    if (this.childrenDB != null)
                    {
                        foreach (Entity child in this.childrenDB)
                        {
                            child.IsComponent = false;
                            this.Children.Add(child.Id, child);
                            child.parent = this;
                        }
                    }
                }

                // 递归设置孩子的Domain
                if (this.children != null)
                {
                    foreach (Entity entity in this.children.Values)
                    {
                        entity.Domain = this.domain;
                    }
                }

                if (this.components != null)
                {
                    foreach (Entity component in this.components.Values)
                    {
                        component.Domain = this.domain;
                    }
                }

                if (!this.IsCreated)
                {
                    this.IsCreated = true;
                    EntitySystemSingleton.Instance.Deserialize(this);
                }
            }
        }

        [BsonElement("Children")]
        [BsonIgnoreIfNull]
        private List<Entity> childrenDB;

        [BsonIgnore]
        private SortedDictionary<long, Entity> children;

        [BsonIgnore]
        public SortedDictionary<long, Entity> Children
        {
            get
            {
                return this.children ??= ObjectPool.Instance.Fetch<SortedDictionary<long, Entity>>();
            }
        }

        private void AddToChildren(Entity entity)
        {
            this.Children.Add(entity.Id, entity);
        }

        private void RemoveFromChildren(Entity entity)
        {
            if (this.children == null)
            {
                return;
            }

            this.children.Remove(entity.Id);

            if (this.children.Count == 0)
            {
                ObjectPool.Instance.Recycle(this.children);
                this.children = null;
            }
        }

        [BsonElement("C")]
        [BsonIgnoreIfNull]
        private List<Entity> componentsDB;

        [BsonIgnore]
        private SortedDictionary<string, Entity> components;

        [BsonIgnore]
        public SortedDictionary<string, Entity> Components
        {
            get
            {
                return this.components ??= ObjectPool.Instance.Fetch<SortedDictionary<string, Entity>>();
            }
        }

        public int ComponentsCount()
        {
            if (this.components == null)
            {
                return 0;
            }
            return this.components.Count;
        }
        
        public int ChildrenCount()
        {
            if (this.children == null)
            {
                return 0;
            }
            return this.children.Count;
        }

        public override void Dispose()
        {
            if (this.IsDisposed)
            {
                return;
            }

            this.IsRegister = false;
            this.InstanceId = 0;

            // 清理Children
            if (this.children != null)
            {
                foreach (Entity child in this.children.Values)
                {
                    child.Dispose();
                }

                this.children.Clear();
                ObjectPool.Instance.Recycle(this.children);
                this.children = null;

                if (this.childrenDB != null)
                {
                    this.childrenDB.Clear();
                    // 创建的才需要回到池中,从db中不需要回收
                    if (this.IsNew)
                    {
                        ObjectPool.Instance.Recycle(this.childrenDB);
                        this.childrenDB = null;
                    }
                }
            }

            // 清理Component
            if (this.components != null)
            {
                foreach (KeyValuePair<string, Entity> kv in this.components)
                {
                    kv.Value.Dispose();
                }

                this.components.Clear();
                ObjectPool.Instance.Recycle(this.components);
                this.components = null;

                // 创建的才需要回到池中,从db中不需要回收
                if (this.componentsDB != null)
                {
                    this.componentsDB.Clear();
                    if (this.IsNew)
                    {
                        ObjectPool.Instance.Recycle(this.componentsDB);
                        this.componentsDB = null;
                    }
                }
            }

            // 触发Destroy事件
            if (this is IDestroy)
            {
                EntitySystemSingleton.Instance.Destroy(this);
            }

            this.domain = null;

            if (this.parent != null && !this.parent.IsDisposed)
            {
                if (this.IsComponent)
                {
                    this.parent.RemoveComponent(this);
                }
                else
                {
                    this.parent.RemoveFromChildren(this);
                }
            }

            this.parent = null;

            base.Dispose();

            if (this.IsFromPool)
            {
                ObjectPool.Instance.Recycle(this);
            }

            status = EntityStatus.None;
        }

        private void AddToComponents(Entity component)
        {
            this.Components.Add(component.GetType().FullName, component);
        }

        private void RemoveFromComponents(Entity component)
        {
            if (this.components == null)
            {
                return;
            }

            this.components.Remove(component.GetType().FullName);

            if (this.components.Count == 0)
            {
                ObjectPool.Instance.Recycle(this.components);
                this.components = null;
            }
        }

        public K GetChild<K>(long id) where K : Entity
        {
            if (this.children == null)
            {
                return null;
            }

            this.children.TryGetValue(id, out Entity child);
            return child as K;
        }

        public void RemoveChild(long id)
        {
            if (this.children == null)
            {
                return;
            }

            if (!this.children.TryGetValue(id, out Entity child))
            {
                return;
            }

            this.children.Remove(id);
            child.Dispose();
        }

        public void RemoveComponent<K>() where K : Entity
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.components == null)
            {
                return;
            }

            Type type = typeof (K);
            Entity c = this.GetComponent(type);
            if (c == null)
            {
                return;
            }

            this.RemoveFromComponents(c);
            c.Dispose();
        }

        private protected void RemoveComponent(Entity component)
        {
            if (this.IsDisposed)
            {
                return;
            }

            if (this.components == null)
            {
                return;
            }

            Entity c = this.GetComponent(component.GetType());
            if (c == null)
            {
                return;
            }

            if (c.InstanceId != component.InstanceId)
            {
                return;
            }

            this.RemoveFromComponents(c);
            c.Dispose();
        }

        public void RemoveComponent(Type type)
        {
            if (this.IsDisposed)
            {
                return;
            }

            Entity c = this.GetComponent(type);
            if (c == null)
            {
                return;
            }

            RemoveFromComponents(c);
            c.Dispose();
        }

        public K GetComponent<K>() where K : Entity
        {
            if (this.components == null)
            {
                return null;
            }

            Entity component;
            if (!this.components.TryGetValue(typeof (K).FullName, out component))
            {
                return default;
            }

            // 如果有IGetComponent接口，则触发GetComponentSystem
            if (this is IGetComponent)
            {
                EntitySystemSingleton.Instance.GetComponent(this, component);
            }

            return (K) component;
        }

        public Entity GetComponent(Type type)
        {
            if (this.components == null)
            {
                return null;
            }

            Entity component;
            if (!this.components.TryGetValue(type.FullName, out component))
            {
                return null;
            }

            // 如果有IGetComponent接口，则触发GetComponentSystem
            if (this is IGetComponent)
            {
                EntitySystemSingleton.Instance.GetComponent(this, component);
            }

            return component;
        }

        private static Entity Create(Type type, bool isFromPool)
        {
            Entity component;
            if (isFromPool)
            {
                component = (Entity) ObjectPool.Instance.Fetch(type);
            }
            else
            {
                component = Activator.CreateInstance(type) as Entity;
            }

            component.IsFromPool = isFromPool;
            component.IsCreated = true;
            component.IsNew = true;
            component.Id = 0;
            return component;
        }

        public Entity AddComponent(Entity component)
        {
            Type type = component.GetType();
            if (this.components != null && this.components.ContainsKey(type.FullName))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            component.ComponentParent = this;

            if (this is IAddComponent)
            {
                EntitySystemSingleton.Instance.AddComponent(this, component);
            }

            return component;
        }

        public Entity AddComponent(Type type, bool isFromPool = false)
        {
            if (this.components != null && this.components.ContainsKey(type.FullName))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = this.Id;
            component.ComponentParent = this;
            EntitySystemSingleton.Instance.Awake(component);

            if (this is IAddComponent)
            {
                EntitySystemSingleton.Instance.AddComponent(this, component);
            }

            return component;
        }

        public K AddComponentWithId<K>(long id, bool isFromPool = false) where K : Entity, IAwake, new()
        {
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(type.FullName))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton.Instance.Awake(component);

            if (this is IAddComponent)
            {
                EntitySystemSingleton.Instance.AddComponent(this, component);
            }

            return component as K;
        }

        public K AddComponentWithId<K, P1>(long id, P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(type.FullName))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton.Instance.Awake(component, p1);

            if (this is IAddComponent)
            {
                EntitySystemSingleton.Instance.AddComponent(this, component);
            }

            return component as K;
        }

        public K AddComponentWithId<K, P1, P2>(long id, P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(type.FullName))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton.Instance.Awake(component, p1, p2);

            if (this is IAddComponent)
            {
                EntitySystemSingleton.Instance.AddComponent(this, component);
            }

            return component as K;
        }

        public K AddComponentWithId<K, P1, P2, P3>(long id, P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            Type type = typeof (K);
            if (this.components != null && this.components.ContainsKey(type.FullName))
            {
                throw new Exception($"entity already has component: {type.FullName}");
            }

            Entity component = Create(type, isFromPool);
            component.Id = id;
            component.ComponentParent = this;
            EntitySystemSingleton.Instance.Awake(component, p1, p2, p3);

            if (this is IAddComponent)
            {
                EntitySystemSingleton.Instance.AddComponent(this, component);
            }

            return component as K;
        }

        public K AddComponent<K>(bool isFromPool = false) where K : Entity, IAwake, new()
        {
            return this.AddComponentWithId<K>(this.Id, isFromPool);
        }

        public K AddComponent<K, P1>(P1 p1, bool isFromPool = false) where K : Entity, IAwake<P1>, new()
        {
            return this.AddComponentWithId<K, P1>(this.Id, p1, isFromPool);
        }

        public K AddComponent<K, P1, P2>(P1 p1, P2 p2, bool isFromPool = false) where K : Entity, IAwake<P1, P2>, new()
        {
            return this.AddComponentWithId<K, P1, P2>(this.Id, p1, p2, isFromPool);
        }

        public K AddComponent<K, P1, P2, P3>(P1 p1, P2 p2, P3 p3, bool isFromPool = false) where K : Entity, IAwake<P1, P2, P3>, new()
        {
            return this.AddComponentWithId<K, P1, P2, P3>(this.Id, p1, p2, p3, isFromPool);
        }

        public Entity AddChild(Entity entity)
        {
            entity.Parent = this;
            return entity;
        }

        public T AddChild<T>(bool isFromPool = false) where T : Entity, IAwake
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        public T AddChild<T, A>(A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        public T AddChild<T, A, B>(A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        public T AddChild<T, A, B, C>(A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = IdGenerater.Instance.GenerateId();
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        public T AddChildWithId<T>(long id, bool isFromPool = false) where T : Entity, IAwake
        {
            Type type = typeof (T);
            T component = Entity.Create(type, isFromPool) as T;
            component.Id = id;
            component.Parent = this;
            EntitySystemSingleton.Instance.Awake(component);
            return component;
        }

        public T AddChildWithId<T, A>(long id, A a, bool isFromPool = false) where T : Entity, IAwake<A>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a);
            return component;
        }

        public T AddChildWithId<T, A, B>(long id, A a, B b, bool isFromPool = false) where T : Entity, IAwake<A, B>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b);
            return component;
        }

        public T AddChildWithId<T, A, B, C>(long id, A a, B b, C c, bool isFromPool = false) where T : Entity, IAwake<A, B, C>
        {
            Type type = typeof (T);
            T component = (T) Entity.Create(type, isFromPool);
            component.Id = id;
            component.Parent = this;

            EntitySystemSingleton.Instance.Awake(component, a, b, c);
            return component;
        }

        public override void BeginInit()
        {
            EntitySystemSingleton.Instance.Serialize(this);
            
            this.componentsDB?.Clear();
            if (this.components != null && this.components.Count != 0)
            {
                foreach (Entity entity in this.components.Values)
                {
                    if (entity is not ISerializeToEntity)
                    {
                        continue;
                    }

                    this.componentsDB ??= ObjectPool.Instance.Fetch<List<Entity>>();
                    this.componentsDB.Add(entity);

                    entity.BeginInit();
                }
            }

            this.childrenDB?.Clear();
            if (this.children != null && this.children.Count != 0)
            {
                foreach (Entity entity in this.children.Values)
                {
                    if (entity is not ISerializeToEntity)
                    {
                        continue;
                    }

                    this.childrenDB ??= ObjectPool.Instance.Fetch<List<Entity>>();
                    this.childrenDB.Add(entity);

                    entity.BeginInit();
                }
            }
        }
    }
}
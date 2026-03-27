using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class ObjectPoolManager : NetworkObjectProviderDefault
{
    private Dictionary<NetworkPrefabId, Stack<NetworkObject>> _freeList = new Dictionary<NetworkPrefabId, Stack<NetworkObject>>();

    protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
    {
        var prefabId = prefab.NetworkTypeId.AsPrefabId;
        if (_freeList.TryGetValue(prefabId, out var stack) && stack.Count > 0)
        {
            var obj = stack.Pop();
            obj.gameObject.SetActive(true);
            return obj;
        }
        
        return base.InstantiatePrefab(runner, prefab);
    }

    protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
    {
        instance.gameObject.SetActive(false);
        if (!_freeList.TryGetValue(prefabId, out var stack))
        {
            stack = new Stack<NetworkObject>();
            _freeList[prefabId] = stack;
        }
        stack.Push(instance);
    }
}

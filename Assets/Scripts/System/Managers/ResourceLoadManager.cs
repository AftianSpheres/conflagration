using System;
using System.Collections.Generic;
using UnityEngine;
using Universe;

/// <summary>
/// Manager that handles resource requests and allows us to line up callbacks to fire off when a requested resource loads.
/// </summary>
public class ResourceLoadManager : Manager<ResourceLoadManager>
{
    /// <summary>
    /// Associates a "canonical" request for an asset with all
    /// callbacks that need to execute when that loads.
    /// </summary>
    private class RequestWithCallback
    {
        public readonly ResourceRequest request;
        public event Action callback;

        public RequestWithCallback (ResourceRequest _request, Action _callback)
        {
            request = _request;
            callback = _callback;
        }

        /// <summary>
        /// Fires off the callback event, if it exists.
        /// </summary>
        public void Fire ()
        {
            callback?.Invoke();
        }
    }

    private Dictionary<UnityEngine.Object, RequestWithCallback> requests = new Dictionary<UnityEngine.Object, RequestWithCallback>(32);
    private UnityEngine.Object[] keys = new UnityEngine.Object[0];
    /// <summary>
    /// Fired when a resource finishes loading, leaving us with no remaining resource requests.
    /// </summary>
    public event Action onBatchedResourceLoadsComplete;
    public bool loading { get { return requests.Count > 0; } }

    /// <summary>
    /// MonoBehaviour.Update ()
    /// </summary>
    void Update()
    {
        // This crashes _bad_ somewhere
        bool redetermineKeys = false;
        for (int i = 0; i < keys.Length; i++)
        {
            if (requests[keys[i]].request.isDone)
            {
                HandleCompletedRequestFor(keys[i]);
                redetermineKeys = true;
            }
        }
        if (redetermineKeys) PopulateKeysArray();
        // If the RLM doesn't presently have any requests to manage, there's no reason to waste time calling Update ().
        if (requests.Count == 0)
        {
            onBatchedResourceLoadsComplete?.Invoke();
            onBatchedResourceLoadsComplete = null;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Gives a resource request to the RLM, along with a callback to fire when it
    /// finishes loading.
    /// </summary>
    public void IssueResourceRequest (ResourceRequest request, Action callback)
    {
        RequestWithCallback rwc;
        if (requests.ContainsKey(request.asset))
        {        
            rwc = requests[request.asset];
            rwc.callback += callback;
           
        }
        else
        {
            rwc = new RequestWithCallback(request, callback);
            if (!rwc.request.isDone)
            {
                requests.Add(rwc.request.asset, rwc);
                PopulateKeysArray();
            }
        }
        if (rwc.request.isDone) HandleCompletedRequestFor(rwc.request.asset);
        else gameObject.SetActive(true);
    }

    /// <summary>
    /// The request for the given asset has finished, so run the
    /// callbacks and remove it from the dictionary of live requests.
    /// </summary>
    private void HandleCompletedRequestFor(UnityEngine.Object asset)
    {
        requests[asset].Fire();
        requests.Remove(asset);
    }

    /// <summary>
    /// Regenerate the key table.
    /// </summary>
    private void PopulateKeysArray ()
    {
        keys = new UnityEngine.Object[requests.Count];
        requests.Keys.CopyTo(keys, 0);
    }
}
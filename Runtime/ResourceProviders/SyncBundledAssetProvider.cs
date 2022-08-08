/*******
 * Source: https://github.com/Unity-Technologies/Addressables-Sample/blob/master/Advanced/Sync%20Addressables/Assets/SyncAddressables/SyncBundledAssetProvider.cs
 ******/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Khepri.AssetDelivery.AssetBundles;
using UnityEngine;
using Khepri.AssetDelivery.ResourceHandlers;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Khepri.AssetDelivery.ResourceProviders
{
	[DisplayName("Assets From Bundles Provider (Sync)")]
	public class SyncBundledAssetProvider : BundledAssetProvider
	{
		private class InternalOp
		{
			private static AssetBundle LoadBundleFromDependecies(IList<object> results)
			{
				return results?.OfType<IAssetBundleResource>()
					.Select(AssetBundleCache.TryLoadBundle)
					.FirstOrDefault();
			}

			AssetBundleRequest req;
			public void Start(ProvideHandle provideHandle)
			{
				Type t = provideHandle.Type;
				List<object> deps = new List<object>();
				provideHandle.GetDependencies(deps);
                		provideHandle.SetWaitForCompletionCallback(WaitForCompletionHandler);
				Debug.LogFormat("[{0}.{1}] path={2} deps={3}", nameof(SyncBundledAssetProvider), nameof(Start), provideHandle.Location.InternalId, deps.Count);
				AssetBundle bundle = LoadBundleFromDependecies(deps);
				Debug.LogFormat("[{0}.{1}] path={2} deps={3} hasBundle={4}", nameof(SyncBundledAssetProvider), nameof(Start), provideHandle.Location.InternalId, deps.Count, bundle != null);
				if (bundle == null)
				{
					provideHandle.Complete<AssetBundle>(null, false, new Exception("Unable to load dependent bundle from location " + provideHandle.Location));
					return;
				}

				object result = null;
				if (t.IsArray)
				{	
					UnityEngine.Debug.Log($"[{nameof(SyncBundledAssetProvider)}] LoadAssetWithSubAssetsAsync(array) Start");
					req = bundle.LoadAssetWithSubAssetsAsync(provideHandle.Location.InternalId, t.GetElementType());
					req.completed += (op) =>
					{
						UnityEngine.Debug.Log($"[{nameof(SyncBundledAssetProvider)}] LoadAssetWithSubAssetsAsync End");
						provideHandle.Complete(req.allAssets, req.allAssets != null, null);
					};
				}
				else if (t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
				{
					UnityEngine.Debug.Log($"[{nameof(SyncBundledAssetProvider)}] LoadAssetWithSubAssetsAsync(IList) Start");
					req = bundle.LoadAssetWithSubAssetsAsync(provideHandle.Location.InternalId, t.GetElementType());
					req.completed += (op) =>
					{
						UnityEngine.Debug.Log($"[{nameof(SyncBundledAssetProvider)}] LoadAssetWithSubAssetsAsync(IList) End");
						provideHandle.Complete(req.allAssets.ToList(), req.allAssets != null, null);
					};
				}
				else
				{
					string subObjectName = null;
					if (ExtractKeyAndSubKey(provideHandle.Location.InternalId, out string mainPath, out string subKey))
					{
						subObjectName = subKey;
						UnityEngine.Debug.Log($"[{nameof(SyncBundledAssetProvider)}] LoadAssetWithSubAssetsAsync(SubKey) Start");
						req = bundle.LoadAssetWithSubAssetsAsync(provideHandle.Location.InternalId, t.GetElementType());
					}
					else
					{
						UnityEngine.Debug.Log($"[{nameof(SyncBundledAssetProvider)}] LoadAssetAsync Start");
						req = bundle.LoadAssetAsync(provideHandle.Location.InternalId, t);
					}
					
					if (string.IsNullOrEmpty(subObjectName))
					{
						req.completed += (op) =>
						{
							UnityEngine.Debug.Log($"[{nameof(SyncBundledAssetProvider)}] LoadAssetAsync End");
							provideHandle.Complete(req.asset, req.asset != null, null);
						};
					}
					else
					{
						req.completed += (op) =>
						{
							UnityEngine.Debug.Log($"[{nameof(SyncBundledAssetProvider)}] LoadAssetWithSubAssetsAsync(SubKey) End");
							foreach (var o in req.allAssets)
							{
								if (o.name == subObjectName)
								{
									if (provideHandle.Type.IsAssignableFrom(o.GetType()))
									{
										provideHandle.Complete(o, o != null, null);
										break;
									}
								}
							}
						};
						
					}
				}
			}

			private bool WaitForCompletionHandler()
			{
				if (req == null)
				    return false;
				if (req.isDone)
				    return true;
				return req.asset != null;
			}
			
			static bool ExtractKeyAndSubKey(object keyObj, out string mainKey, out string subKey)
			{
				var key = keyObj as string;
				if (key != null)
				{
					var i = key.IndexOf('[');
					if (i > 0)
					{
						var j = key.LastIndexOf(']');
						if (j > i)
						{
							mainKey = key.Substring(0, i);
							subKey = key.Substring(i + 1, j - (i + 1));
							return true;
						}
					}
				}
				mainKey = null;
				subKey = null;
				return false;
			}
		}
	
		public override void Provide(ProvideHandle provideHandle)
		{
			Debug.LogFormat("[{0}.{1}] path={2}", nameof(SyncBundledAssetProvider), nameof(Provide), provideHandle.Location.InternalId);
			new InternalOp().Start(provideHandle);
		}
	}
}

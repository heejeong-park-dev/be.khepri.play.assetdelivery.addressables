﻿/*******
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

			public void Start(ProvideHandle provideHandle)
			{
				Type t = provideHandle.Type;
				List<object> deps = new List<object>();
				provideHandle.GetDependencies(deps);
				Debug.LogFormat("[{0}.{1}] path={2} deps={3}", nameof(SyncBundledAssetProvider), nameof(Start), provideHandle.Location.InternalId, deps.Count);
				AssetBundle bundle = LoadBundleFromDependecies(deps);
				Debug.LogFormat("[{0}.{1}] path={2} deps={3} hasBundle={4}", nameof(SyncBundledAssetProvider), nameof(Start), provideHandle.Location.InternalId, deps.Count, bundle != null);
				if (bundle == null)
				{
					provideHandle.Complete<AssetBundle>(null, false, new Exception("Unable to load dependent bundle from location " + provideHandle.Location));
					return;
				}

				object result = null;
				AssetBundleRequest req;
				if (t.IsArray)
				{	
					req = bundle.LoadAssetWithSubAssetsAsync(provideHandle.Location.InternalId, t.GetElementType());
					req.completed += (op) =>
					{
						provideHandle.Complete(req.allAssets, result != null, null);
					};
				}
				else if (t.IsGenericType && typeof(IList<>) == t.GetGenericTypeDefinition())
				{
					req = bundle.LoadAssetWithSubAssetsAsync(provideHandle.Location.InternalId, t.GetElementType());
					req.completed += (op) =>
					{
						provideHandle.Complete(req.allAssets.ToList(), result != null, null);
					};
				}
				else
				{
					req = bundle.LoadAssetAsync(provideHandle.Location.InternalId, t);
					req.completed += (op) =>
					{
						provideHandle.Complete(req.asset, result != null, null);
					};
				}
				
			}
		}
	
		public override void Provide(ProvideHandle provideHandle)
		{
			Debug.LogFormat("[{0}.{1}] path={2}", nameof(SyncBundledAssetProvider), nameof(Provide), provideHandle.Location.InternalId);
			new InternalOp().Start(provideHandle);
		}
	}
}

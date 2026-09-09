using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System.Threading;
using VContainer.Unity;
using System;
using R3;

namespace ElementalBlacksmithStory.Core
{
    public class WeaponSpriteLoader : BaseSpriteLoader
    {
        protected override string AtlasAddress => "WeaponAtlas";
    }
}
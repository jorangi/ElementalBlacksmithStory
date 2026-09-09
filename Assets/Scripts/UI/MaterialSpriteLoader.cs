using System;
using System.Threading;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using VContainer.Unity;
using R3;

namespace ElementalBlacksmithStory.Core
{
    public class MaterialSpriteLoader : BaseSpriteLoader
    {
        protected override string AtlasAddress => "MaterialAtlas";
    }
}
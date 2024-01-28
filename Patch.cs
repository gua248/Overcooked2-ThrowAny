//using AssetBundles;
using HarmonyLib;
using OC2ThrowAny.Extension;
using Team17.Online.Multiplayer.Messaging;
using UnityEngine;

namespace OC2ThrowAny
{
    public static class Patch
    {
        public static void PatchInternal(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method("ServerMessenger:ResumeChefPositionSync"), new HarmonyMethod(typeof(Patch).GetMethod("ServerMessengerResumeChefPositionSyncPrefix")), null);
        }

        public static void ServerMessengerResumeChefPositionSyncPrefix(GameObject _object, ChefPositionMessage _data)
        {
            _object.GetComponent<ServerPlayerAttachment>()?.CorrectMessage(_data.WorldObject);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ServerWorldObjectSynchroniser), "PopulateMessage")]
        public static void ServerWorldObjectSynchroniserPopulateMessagePatch(ServerWorldObjectSynchroniser __instance)
        {
            __instance.GetComponent<ServerPlayerAttachment>()?.CorrectMessage(__instance.get_m_ServerData());
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientKitchenLoader), "StartEntities")]
        public static void ClientKitchenLoaderStartEntitiesPatch()
        {
            if (!ThrowAnySettings.enabledChef) return;
            GameObject ceiling = GameObject.Find("Ceiling");
            if (ceiling != null)
                ceiling.transform.position = ceiling.transform.position.AddY(2.0f);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerControlsHelper), "PlaceHeldItem_Client")]
        public static bool PlayerControlsHelperPlaceHeldItem_ClientPatch(PlayerControls _control)
        {
            ICarrier carrier = _control.gameObject.RequireInterface<ICarrier>();
            GameObject x = carrier.InspectCarriedItem();
            IClientHandlePlacement iHandlePlacement = _control.CurrentInteractionObjects.m_iHandlePlacement;
            return x.GetComponent<ServerPlayerAttachment>() == null || iHandlePlacement == null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerAttachmentCatchingProxy), "AttemptToCatch")]
        public static bool ServerAttachmentCatchingProxyAttemptToCatchPatch(GameObject _object)
        {
            return _object.GetComponent<ServerPlayerAttachment>() == null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClientPlayerControlsImpl_Default), "OnDashCollision")]
        public static bool ClientPlayerControlsImpl_DefaultOnDashCollisionPatch(ClientPlayerControlsImpl_Default __instance, PlayerControls _otherPlayer)
        {
            if (!ThrowAnySettings.enabledChef) return true;
            var serverThrowableItem = __instance.GetComponent<ServerThrowableItem>();
            if (serverThrowableItem != null && serverThrowableItem.IsFlying()) return false;
            var serverPlayerAttachment = __instance.GetComponent<ServerPlayerAttachment>();
            if (serverPlayerAttachment != null && serverPlayerAttachment.m_isHeld) return false;
            
            serverThrowableItem = _otherPlayer.GetComponent<ServerThrowableItem>();
            if (serverThrowableItem != null && serverThrowableItem.IsFlying()) return false;
            serverPlayerAttachment = _otherPlayer.GetComponent<ServerPlayerAttachment>();
            if (serverPlayerAttachment != null && serverPlayerAttachment.m_isHeld) return false;
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerThrowableItem), "HandleThrow")]
        public static void ServerThrowableItemHandleThrowPatch(ServerThrowableItem __instance, ref Vector2 _directionXZ)
        {
            if (__instance.GetComponent<ServerPlayerAttachment>() != null)
                __instance.GetComponent<ClientPlayerControlsImpl_Default>().set_m_dashTimer(0.4f);
        }

        /* 
         * Sending messages to clients without this MOD can cause ArrayIndexOutOfBoundsException
         * at ClientSynchronisationReceiver::OnEntityEventMessageReceived
         * Thus we drop messaging.
         */
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ServerThrowableItem), "SendStateMessage")]
        public static bool ServerThrowableItemSendStateMessagePatch(ServerThrowableItem __instance)
        {
            return __instance.GetComponent<ThrowableItem>().m_throwParticle != null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ClientChefSynchroniser), "LocalChefReceiveData")]
        public static void ClientChefSynchroniserLocalChefReceiveDataPatch(ClientChefSynchroniser __instance, ChefPositionMessage dataReceived)
        {
            __instance.GetComponent<ClientPlayerAttachment>()?.LocalChefReceiveData(dataReceived);
        }

        // ThrowableItem has an attribute [RequireComponent(typeof(PhysicalAttachment))]
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PhysicalAttachment), "Awake")]
        public static bool PhysicalAttachmentAwakePatch(PhysicalAttachment __instance)
        {
            if (__instance.GetComponent<PlayerControls>() != null)
            {
                __instance.enabled = false;
                return false;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControls), "Start")]
        public static void AddComponentsForPlayer(PlayerControls __instance)
        {
            if (!ThrowAnySettings.enabledChef) return;
            GameObject gameObject = __instance.gameObject;
            if (gameObject.GetComponent<ThrowableItem>() != null) return;
            ThrowableItem throwableItem = gameObject.AddComponent<ThrowableItem>();
            throwableItem.m_throwParticle = null;
            var multiplayerController = GameUtils.RequestManager<MultiplayerController>();
            bool isServer = multiplayerController == null || multiplayerController.IsServer();
            if (isServer)
            {
                var serverThrowableItem = gameObject.AddComponent<ServerThrowableItem>();
                serverThrowableItem.SetSynchronisedComponent(throwableItem);
                serverThrowableItem.StartSynchronising(throwableItem);
                gameObject.AddComponent<ServerCarryablePlayer>();
                var component = gameObject.AddComponent<ServerPlayerAttachment>();
                EntitySerialisationRegistry.GetEntry(gameObject).m_ServerSynchronisedComponents.Add(component);
                // for UpdateSynchronising() being called
            }
            var clientThrowableItem = gameObject.AddComponent<ClientThrowableItem>();
            clientThrowableItem.SetSynchronisedComponent(throwableItem);
            clientThrowableItem.StartSynchronising(throwableItem);
            gameObject.AddComponent<ClientCarryablePlayer>();
            gameObject.AddComponent<ClientPlayerAttachment>();
            ComponentCache<IClientHandlePickup>.CacheObject(gameObject);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientIngredientContainer), "StartSynchronising")]
        public static void AddComponentsForCoalBucket(ClientIngredientContainer __instance)
        {
            if (__instance.GetComponent<ContainerHeatTransferBehaviour>() != null)
                AddComponentsForItem(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientCookingHandler), "StartSynchronising")]
        [HarmonyPatch(typeof(ClientMixingHandler), "StartSynchronising")]
        [HarmonyPatch(typeof(ClientPlate), "StartSynchronising")]
        [HarmonyPatch(typeof(ClientPlateStackBase), "StartSynchronising")]
        [HarmonyPatch(typeof(ClientLadleContainer), "StartSynchronising")]
        public static void AddComponentsForItem(Component __instance)
        {
            if (!ThrowAnySettings.enabledItem) return;
            GameObject gameObject = __instance.gameObject;
            if (gameObject.GetComponent<ThrowableItem>() != null) return;
            ThrowableItem throwableItem = gameObject.AddComponent<ThrowableItem>();
            throwableItem.m_throwParticle = null;
            var multiplayerController = GameUtils.RequestManager<MultiplayerController>();
            bool isServer = multiplayerController == null || multiplayerController.IsServer();
            if (isServer)
            {
                var serverThrowableItem = gameObject.AddComponent<ServerThrowableItem>();
                serverThrowableItem.SetSynchronisedComponent(throwableItem);
                serverThrowableItem.StartSynchronising(throwableItem);
            }
            var clientThrowableItem = gameObject.AddComponent<ClientThrowableItem>();
            clientThrowableItem.SetSynchronisedComponent(throwableItem);
            clientThrowableItem.StartSynchronising(throwableItem);
        }

        //static GameObject throwParticle = null;
        //static GameObject FindSprayEffectPrefab()
        //{
        //    if (throwParticle == null)
        //    {
        //        LoadedAssetBundle bundle = AssetBundleManager.GetLoadedAssetBundle("bundle47", out string _);
        //        var prefab = (GameObject)bundle.m_AssetBundle.LoadAsset("assets/prefabs/shared_kitchen/dispenser_crate_03.prefab");
        //        throwParticle = prefab.GetComponent<PickupItemSpawner>().m_itemPrefab.GetComponent<ThrowableItem>().m_throwParticle;
        //    }
        //    return throwParticle;
        //}

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ServerPreparationContainer), "AllowThrowing")]
        public static void ServerPreparationContainerAllowThrowingPatch(ref bool __result)
        {
            if (ThrowAnySettings.enabledItem) __result = true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClientPreparationContainer), "AllowThrowing")]
        public static void ClientPreparationContainerAllowThrowingPatch(ref bool __result)
        {
            if (ThrowAnySettings.enabledItem) __result = true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FrontendCoopTabOptions), "OnOnlinePublicClicked")]
        [HarmonyPatch(typeof(FrontendVersusTabOptions), "OnOnlinePublicClicked")]
        public static bool OnOnlinePublicClickedPatch()
        {
            ThrowAnySettings.enabledItem = false;
            ThrowAnySettings.enabledChef = false;
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(T17TabPanel), "OnTabSelected")]
        public static void T17TabPanelOnTabSelectedPatch()
        {
            ThrowAnySettings.AddUI();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ToggleOption), "OnToggleButtonPressed")]
        public static bool ToggleOptionOnToggleButtonPressedPatch(ToggleOption __instance, bool bValue)
        {
            if (__instance == ThrowAnySettings.throwItemOption)
            {
                ThrowAnySettings.enabledItem = bValue;
                return false;
            }
            if (__instance == ThrowAnySettings.throwChefOption)
            {
                ThrowAnySettings.enabledChef = bValue;
                return false;
            }
            return true;
        }
    }
}

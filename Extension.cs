using HarmonyLib;
using System.Reflection;
using Team17.Online.Multiplayer.Messaging;
using UnityEngine;
using UnityEngine.UI;

namespace OC2ThrowAny.Extension
{
    static class ServerWorldObjectSynchroniserExtension
    {
        static readonly FieldInfo fieldInfo_m_ServerData = AccessTools.Field(typeof(ServerWorldObjectSynchroniser), "m_ServerData");

        public static WorldObjectMessage get_m_ServerData(this ServerWorldObjectSynchroniser instance)
        {
            return (WorldObjectMessage)fieldInfo_m_ServerData.GetValue(instance);
        }
    }

    static class ClientPlayerControlsImpl_DefaultExtension
    {
        static readonly FieldInfo fieldInfo_m_dashTimer = AccessTools.Field(typeof(ClientPlayerControlsImpl_Default), "m_dashTimer");

        public static void set_m_dashTimer(this ClientPlayerControlsImpl_Default instance, float value)
        {
            fieldInfo_m_dashTimer.SetValue(instance, value);
        }
    }

    static class MultiplayerControllerExtension
    {
        static readonly MethodInfo methodInfo_IsServer = AccessTools.Method(typeof(MultiplayerController), "IsServer");

        public static bool IsServer(this MultiplayerController instance)
        {
            return (bool)methodInfo_IsServer.Invoke(instance, null);
        }
    }

    public static class FrontendRootMenuExtension
    {
        static readonly FieldInfo fieldInfo_m_CurrentGamepadUser = AccessTools.Field(typeof(FrontendRootMenu), "m_CurrentGamepadUser");
        static readonly MethodInfo methodInfo_OnMenuHide = AccessTools.Method(typeof(FrontendRootMenu), "OnMenuHide");
        static readonly MethodInfo methodInfo_OnMenuShow = AccessTools.Method(typeof(FrontendRootMenu), "OnMenuShow");

        public static GamepadUser get_m_CurrentGamepadUser(this FrontendRootMenu instance)
        {
            return (GamepadUser)fieldInfo_m_CurrentGamepadUser.GetValue(instance);
        }

        public static void OnMenuShow(this FrontendRootMenu instance, BaseMenuBehaviour menu)
        {
            methodInfo_OnMenuShow.Invoke(instance, new object[] { menu });
        }

        public static void OnMenuHide(this FrontendRootMenu instance, BaseMenuBehaviour menu)
        {
            methodInfo_OnMenuHide.Invoke(instance, new object[] { menu });
        }
    }

    public static class FrontendOptionsMenuExtension
    {
        static readonly FieldInfo fieldInfo_m_ConsoleTopSelectable = AccessTools.Field(typeof(FrontendOptionsMenu), "m_ConsoleTopSelectable");
        static readonly FieldInfo fieldInfo_m_SyncOptions = AccessTools.Field(typeof(FrontendOptionsMenu), "m_SyncOptions");
        static readonly FieldInfo fieldInfo_m_VersionString = AccessTools.Field(typeof(FrontendOptionsMenu), "m_VersionString");
    
        public static void set_m_VersionString(this FrontendOptionsMenu instance, T17Text text)
        {
            fieldInfo_m_VersionString.SetValue(instance, text);
        }

        public static void set_m_ConsoleTopSelectable(this FrontendOptionsMenu instance, Selectable selectable)
        {
            fieldInfo_m_ConsoleTopSelectable.SetValue(instance, selectable);
        }

        public static ISyncUIWithOption[] get_m_SyncOptions(this FrontendOptionsMenu instance)
        {
            return (ISyncUIWithOption[])fieldInfo_m_SyncOptions.GetValue(instance);
        }

        public static void set_m_SyncOptions(this FrontendOptionsMenu instance, ISyncUIWithOption[] options)
        {
            fieldInfo_m_SyncOptions.SetValue(instance, options);
        }
    }

    public static class BaseUIOptionExtension
    {
        static readonly FieldInfo fieldInfo_m_OptionType = AccessTools.Field(typeof(BaseUIOption<IOption>), "m_OptionType");
        static readonly FieldInfo fieldInfo_m_Option = AccessTools.Field(typeof(BaseUIOption<IOption>), "m_Option");

        public static void set_m_OptionType(this BaseUIOption<IOption> instance, OptionsData.OptionType type)
        {
            fieldInfo_m_OptionType.SetValue(instance, type);
        }

        public static void set_m_Option(this BaseUIOption<IOption> instance, IOption option)
        {
            fieldInfo_m_Option.SetValue(instance, option);
        }
    }
}

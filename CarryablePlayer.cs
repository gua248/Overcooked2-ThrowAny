using OC2ThrowAny.Extension;
using System;
using Team17.Online.Multiplayer.Messaging;
using UnityEngine;

namespace OC2ThrowAny
{
    public class ServerCarryablePlayer : ServerCarryableItem
    {
        public void Awake()
        {
            m_carrier = base.gameObject.GetComponent<ServerPlayerAttachmentCarrier>();
            m_attachment = base.gameObject.GetComponent<ServerPlayerAttachment>();
            m_animationDecisions = base.gameObject.GetComponent<PlayerAnimationDecisions>();
            m_controls = base.gameObject.GetComponent<PlayerControls>();
        }

        public override bool CanHandlePickup(ICarrier _carrier)
        {
            return 
                base.isActiveAndEnabled && 
                !m_attachment.IsAttached() && 
                m_carrier.InspectCarriedItem() != _carrier.AccessGameObject() &&
                !m_animationDecisions.IsInCannon();
        }

        public override void HandlePickup(ICarrier _carrier, Vector2 _directionXZ)
        {
            EndInteractableSession();
            _carrier.CarryItem(base.gameObject);
        }

        void EndInteractableSession()
        {
            if (m_controls == null) return;
            var currentlyInteracting = m_controls.GetCurrentlyInteracting();
            if (currentlyInteracting == null) return;
            var serverSessionInteractable = 
                currentlyInteracting.GetComponent<ServerPushableObject>() as ServerSessionInteractable ?? 
                currentlyInteracting.GetComponent<ServerTerminal>() as ServerSessionInteractable;
            serverSessionInteractable?.EndSession();
        }

        ServerPlayerAttachmentCarrier m_carrier;
        ServerPlayerAttachment m_attachment;
        PlayerAnimationDecisions m_animationDecisions;
        PlayerControls m_controls;
    }

    public class ClientCarryablePlayer : ClientCarryableItem { }

    public class ServerPlayerAttachment : ServerSynchroniserBase, IAttachment
    {
        public void Awake()
        {
            m_carrier = base.gameObject.RequireComponent<ServerPlayerAttachmentCarrier>();
            m_playerControls = base.gameObject.RequireComponent<PlayerControls>();
            m_ServerTeleportablePlayer = base.gameObject.RequireComponent<ServerTeleportablePlayer>();
            m_transform = base.transform;
            m_disableDynamicReparenting = GameUtils.GetLevelConfig().m_disableDynamicParenting;
            m_originalParent = base.transform.parent;
        }

        public override void UpdateSynchronising()
        {
            if (m_isHeld)
            {
                m_transform.localPosition = m_carrier.InspectCarriedItem(PlayerAttachTarget.Back) == null ? holdSpaceDefault : holdSpaceBackpack;
                m_transform.localRotation = Quaternion.identity;
                if (m_playerControls.m_bRespawning || 
                    m_ServerTeleportablePlayer != null && m_ServerTeleportablePlayer.IsTeleporting())
                {
                    (m_holder as PlayerAttachmentCarrier)?.GetComponent<ServerPlayerAttachmentCarrier>().TakeItem();
                }
            }
            else if (m_detachedTimer >= 0)
            {
                float deltaTime = TimeManager.GetDeltaTime(base.gameObject);
                m_detachedTimer -= deltaTime;
                if (m_detachedTimer < 0)
                    m_playerControls.m_bApplyGravity = true;
            }
        }

        public void RegisterAttachChangedCallback(AttachChangedCallback _callback)
        {
            m_attachChangedCallback = (AttachChangedCallback)Delegate.Combine(m_attachChangedCallback, _callback);
        }

        public void UnregisterAttachChangedCallback(AttachChangedCallback _callback)
        {
            m_attachChangedCallback = (AttachChangedCallback)Delegate.Remove(m_attachChangedCallback, _callback);
        }

        public void Attach(IParentable _parentable)
        {
            m_originalParent = base.transform.parent;
            m_transform.SetParent(_parentable.GetAttachPoint(base.gameObject));
            Vector3 lossyScale = m_transform.lossyScale;
            Vector3 b = new Vector3(1f / lossyScale.x, 1f / lossyScale.y, 1f / lossyScale.z);
            m_transform.localScale = m_transform.localScale.MultipliedBy(b);
            m_transform.localPosition = m_carrier.InspectCarriedItem(PlayerAttachTarget.Back) == null ? holdSpaceDefault : holdSpaceBackpack;
            m_transform.localRotation = Quaternion.identity;
            m_isHeld = true;
            m_holder = _parentable;
            m_playerControls.m_bApplyGravity = false;
            m_playerControls.Motion.SetKinematic(true);
            DynamicLandscapeParenting dynamicLandscapeParenting = base.gameObject.RequestComponent<DynamicLandscapeParenting>();
            if (dynamicLandscapeParenting != null)
            {
                dynamicLandscapeParenting.enabled = false;
            }
            OnAttachChanged(_parentable);
        }

        public void Detach()
        {
            if (m_transform.parent == m_holder.GetAttachPoint(base.gameObject))
            {
                m_transform.SetParent(m_originalParent);
                m_transform.localScale = Vector3.one;
            }
            m_isHeld = false;
            m_holder= null;
            m_detachedTimer = delayGravity;
            m_playerControls.Motion.SetKinematic(false);
            DynamicLandscapeParenting dynamicLandscapeParenting = base.gameObject.RequestComponent<DynamicLandscapeParenting>();
            if (dynamicLandscapeParenting != null)
            {
                dynamicLandscapeParenting.enabled = true;
            }
            OnAttachChanged(null);
        }

        public bool IsAttached()
        {
            return m_isHeld;
        }

        public GameObject AccessGameObject()
        {
            return base.gameObject;
        }

        public Rigidbody AccessRigidbody()
        {
            return null;
        }

        public RigidbodyMotion AccessMotion()
        {
            return m_playerControls.Motion;
        }

        private void OnAttachChanged(IParentable _parentable)
        {
            m_attachChangedCallback(_parentable);
        }

        public void CorrectMessage(WorldObjectMessage message)
        {
            if (!m_disableDynamicReparenting || !m_isHeld) return;
            message.LocalPosition = m_originalParent == null ? 
                m_transform.position : m_transform.position - m_originalParent.position;
            message.ParentEntityID = 1023U;
            message.LocalRotation = m_transform.rotation;
        }

        private ServerPlayerAttachmentCarrier m_carrier;
        static readonly Vector3 holdSpaceDefault = new Vector3(0f, 0.3f, 0.3f);
        static readonly Vector3 holdSpaceBackpack = new Vector3(0f, 0.3f, 1.0f);
        const float delayGravity = 0.2f;
        private PlayerControls m_playerControls;
        private ServerTeleportablePlayer m_ServerTeleportablePlayer;
        private bool m_isHeld = false;
        private IParentable m_holder;
        private float m_detachedTimer;
        private AttachChangedCallback m_attachChangedCallback = delegate (IParentable _parentable) { };
        private Transform m_transform;
        private bool m_disableDynamicReparenting;
        private Transform m_originalParent;
    }

    public class ClientPlayerAttachment : ClientSynchroniserBase, IClientAttachment
    {
        public void Awake()
        {
            m_playerControls = base.gameObject.RequireComponent<PlayerControls>();
        }

        public IClientSidePredicted GetClientSidePrediction() { return null; }

        public void SetClientSidePrediction(CreateClientSidePredictionCallback prediction) { }

        public bool IsAttached() { return false; }

        public GameObject AccessGameObject() { return base.gameObject; }

        public Rigidbody AccessRigidbody() { return null; }

        public void RegisterAttachChangedCallback(AttachChangedCallback _callback)
        {
            m_attachChangedCallback = (AttachChangedCallback)Delegate.Combine(m_attachChangedCallback, _callback);
        }

        public void UnregisterAttachChangedCallback(AttachChangedCallback _callback)
        {
            m_attachChangedCallback = (AttachChangedCallback)Delegate.Remove(m_attachChangedCallback, _callback);
        }

        public void LocalChefReceiveData(ChefPositionMessage dataReceived)
        {
            WorldObjectMessage worldObject = dataReceived.WorldObject;
            bool newHeld = worldObject.ParentEntityID == 1023U;
            if (m_isHeld != newHeld)
            {
                m_playerControls.m_bApplyGravity = !newHeld;
                m_isHeld = newHeld;
            }
        }

        private PlayerControls m_playerControls;
        private AttachChangedCallback m_attachChangedCallback = delegate (IParentable _parentable) { };
        private bool m_isHeld = false;
    }
}

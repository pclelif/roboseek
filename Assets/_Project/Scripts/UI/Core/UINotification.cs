using System;
using System.Collections.Generic;
using UnityEngine;

namespace Robot.UI.Core
{
    /// <summary>
    /// Notification Manager / Container managing active toasts on the Notifications Canvas layer.
    /// </summary>
    public class UINotification : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameObject notificationPrefab;
        [SerializeField] private Transform containerTransform;
        [SerializeField] private int maxSimultaneousNotifications = 4;
        [SerializeField] private float defaultDuration = 3.5f;

        private readonly List<UINotificationItem> activeItems = new List<UINotificationItem>();

        private void Awake()
        {
            if (containerTransform == null) containerTransform = transform;
        }

        /// <summary>
        /// Displays a toast notification on screen.
        /// </summary>
        public UINotificationItem ShowNotification(string title, string message, NotificationType type = NotificationType.Info, float duration = -1f)
        {
            if (notificationPrefab == null)
            {
                Debug.LogWarning("[UINotification] Notification Prefab is not assigned.");
                return null;
            }

            if (duration <= 0f) duration = defaultDuration;

            // Enforce max notifications limit
            if (activeItems.Count >= maxSimultaneousNotifications && activeItems.Count > 0)
            {
                var oldest = activeItems[0];
                activeItems.RemoveAt(0);
                if (oldest != null) Destroy(oldest.gameObject);
            }

            GameObject instance = Instantiate(notificationPrefab, containerTransform != null ? containerTransform : transform);
            UINotificationItem item = instance.GetComponent<UINotificationItem>();

            if (item != null)
            {
                activeItems.Add(item);
                item.Initialize(title, message, type, duration, OnItemDismissed);
            }

            return item;
        }

        private void OnItemDismissed(UINotificationItem item)
        {
            if (activeItems.Contains(item))
            {
                activeItems.Remove(item);
            }
        }

        /// <summary>
        /// Clears all currently visible notifications.
        /// </summary>
        public void ClearAll()
        {
            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                if (activeItems[i] != null) Destroy(activeItems[i].gameObject);
            }
            activeItems.Clear();
        }
    }
}

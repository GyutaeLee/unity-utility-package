using UnityEngine;

namespace qbot.Utility
{
    public static class GameObjectFinder
    {
#region Public functions

        /// <summary>
        /// Find the first GameObject that is a child of 'parent' that has the same name as 'name'.
        /// </summary>
        /// <param name="parent">Parent GameObject of the GameObject you are looking for</param>
        /// <param name="name">The name of the GameObject you are looking for</param>
        /// <param name="includeInactive">Whether to look for disabled GameObjects as well</param>
        /// <returns>Found GameObject</returns>
        public static GameObject GetGameObjectInAllChild(GameObject parent, string name, bool includeInactive)
        {
            if (parent == null)
            {
                Debug.Log("GetGameObjectInAllChild: parent is null.");
                return null;
            }

            var allChildren = parent.GetComponentsInChildren<Transform>(includeInactive);

            foreach (var child in allChildren)
            {
                if (child.name == name)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// Find the first Component that is a child of 'parent' that has the same name as 'name'.
        /// </summary>
        /// <param name="parent">Parent GameObject of the GameObject you are looking for</param>
        /// <param name="name">The name of the GameObject you are looking for</param>
        /// <param name="includeInactive">Whether to look for disabled GameObjects as well</param>
        /// <returns>Found Component</returns>
        public static T FindComponentInAllChild<T>(GameObject parent, string name, bool includeInactive)
        {
            if (parent == null)
            {
                Debug.Log("FindComponentInAllChild: parent is null.");
                return default;
            }

            var gameObject = GetGameObjectInAllChild(parent, name, includeInactive);
            if (gameObject == null)
                return default;

            return gameObject.GetComponent<T>();
        }

#endregion
    }
}
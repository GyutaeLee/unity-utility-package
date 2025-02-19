using System.Collections.Generic;
using UnityEngine;

namespace qbot.Utility
{
    public static class GameObjectFinder
    {
        /// <summary>
        /// Find the first child GameObject with the specified name under the given parent GameObject.
        /// This method searches recursively through all children.
        /// </summary>
        /// <param name="parent">The parent GameObject to start searching from.</param>
        /// <param name="name">The name of the GameObject to find.</param>
        /// <param name="includeInactive">Whether to include inactive GameObjects in the search.</param>
        /// <returns>The first matching GameObject, or null if not found.</returns>
        public static GameObject GetGameObjectInAllChild(GameObject parent, string name, bool includeInactive)
        {
            if (parent == null)
            {
                Debug.Log("GetGameObjectInAllChild: parent is null.");
                return null;
            }

            foreach (Transform child in parent.transform)
            {
                if (includeInactive == false && child.gameObject.activeSelf == false) 
                    continue;
                
                if (child.name == name)
                    return child.gameObject;
                
                var found = GetGameObjectInAllChild(child.gameObject, name, includeInactive);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Find the first component of the specified type in a child GameObject with the given name.
        /// This method searches recursively through all children.
        /// </summary>
        /// <typeparam name="T">The type of the component to find.</typeparam>
        /// <param name="parent">The parent GameObject to start searching from.</param>
        /// <param name="name">The name of the GameObject containing the component to find.</param>
        /// <param name="includeInactive">Whether to include inactive GameObjects in the search.</param>
        /// <returns>The first matching component, or the default value of T if not found.</returns>
        public static T FindComponentInAllChild<T>(GameObject parent, string name, bool includeInactive) where T : Component
        {
            if (parent == null)
            {
                Debug.Log("FindComponentInAllChild: parent is null.");
                return null;
            }

            var gameObject = GetGameObjectInAllChild(parent, name, includeInactive);
            if (gameObject != null) 
                return gameObject.GetComponent<T>();
            
            Debug.Log($"FindComponentInAllChild: No GameObject named '{name}' found under '{parent.name}'.");
            return null;
        }
        
        /// <summary>
        /// Find all components of the specified type in all child GameObjects of the given parent GameObject.
        /// This method searches recursively through all children.
        /// </summary>
        public static List<T> FindComponentsInAllChild<T>(GameObject parent, bool includeInactive) where T : Component
        {
            var components = new List<T>();

            if (parent == null)
            {
                Debug.Log("FindComponentsInAllChild: parent is null.");
                return components;
            }
            
            foreach (Transform child in parent.transform)
            {
                if (includeInactive == false && child.gameObject.activeSelf == false)
                    continue;
            
                components.AddRange(child.GetComponents<T>());
                components.AddRange(FindComponentsInAllChild<T>(child.gameObject, includeInactive));
            }

            return components;
        }
    }
}
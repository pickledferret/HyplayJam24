using System.Collections.Generic;
using System.Linq;
using HYPLAY.Leaderboards.Runtime;
using UnityEditor;
using UnityEngine;

namespace HYPLAY.Leaderboards
{
    [CustomPropertyDrawer(typeof(HyplayLeaderboard))]
    public class HyplayLeaderboardPropertyDrawer : PropertyDrawer
    {
        private bool hasChanges = false; // Track if changes were made
        private static List<string> leaderboardNames = null; // Cache for leaderboard names
        private static double lastRefreshTime = 0; // Track the last refresh time

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject is HyplayLeaderboards)
            {
                DrawFullInspector(position, property, label);
            } else
            {
                DrawDropdown(position, property, label);
            }
        }

        private void DrawDropdown(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            // Refresh leaderboard names if cache is empty or it's been a while
            if (leaderboardNames == null || EditorApplication.timeSinceStartup - lastRefreshTime > 5) 
            {
                RefreshLeaderboardNames();
            }

            // Add "Select a leaderboard" option at the beginning
            var dropdownOptions = new List<string> { "Select a leaderboard" };
            dropdownOptions.AddRange(leaderboardNames);

            // Find the index of the currently selected leaderboard, accounting for the new option
            int selectedIndex = property.objectReferenceValue != null 
                ? leaderboardNames.IndexOf(((HyplayLeaderboard)property.objectReferenceValue).name) + 1 // Offset by 1 due to the added option
                : 0; // "Select a leaderboard" is at index 0

            // Draw the dropdown
            EditorGUI.BeginChangeCheck();
            int newSelectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, dropdownOptions.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                // Update the selected leaderboard if changed
                if (newSelectedIndex == 0) 
                {
                    // "Select a leaderboard" was chosen
                    property.objectReferenceValue = null;
                }
                else if (newSelectedIndex > 0 && newSelectedIndex <= leaderboardNames.Count)
                {
                    var allLeaderboards = Resources.FindObjectsOfTypeAll<HyplayLeaderboard>();
                    var selectedLeaderboard = allLeaderboards.FirstOrDefault(lb => lb.name == leaderboardNames[newSelectedIndex - 1]); // Adjust index back to original list
                    property.objectReferenceValue = selectedLeaderboard;
                }
            }
            EditorGUI.EndProperty();
        }

        private void DrawFullInspector(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

                var serializedObject = new SerializedObject(property.objectReferenceValue);
                // Calculate field positions dynamically
                var nameRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                var descriptionRect = new Rect(position.x, nameRect.y + EditorGUIUtility.singleLineHeight + 5,
                    position.width, EditorGUIUtility.singleLineHeight);
                var idRect = new Rect(position.x, descriptionRect.y + EditorGUIUtility.singleLineHeight + 5,
                    position.width, EditorGUIUtility.singleLineHeight);
                var secretKeyRect = new Rect(position.x, idRect.y + EditorGUIUtility.singleLineHeight + 5,
                    position.width, EditorGUIUtility.singleLineHeight);
                var buttonRect = new Rect(position.x, secretKeyRect.y + EditorGUIUtility.singleLineHeight + 10,
                    position.width, EditorGUIUtility.singleLineHeight);

                // Find properties
                var nameProperty = serializedObject.FindProperty("name");
                var descriptionProperty = serializedObject.FindProperty("description");
                var idProperty = serializedObject.FindProperty("id");
                var secretKeyProperty = serializedObject.FindProperty("secretKey");

                // Draw fields
                EditorGUI.BeginChangeCheck(); // Start checking for changes
                EditorGUI.PropertyField(nameRect, nameProperty, new GUIContent("Name"));
                EditorGUI.PropertyField(descriptionRect, descriptionProperty, new GUIContent("Description"));
                if (EditorGUI.EndChangeCheck()) // End checking and update flag
                {
                    hasChanges = true;
                    serializedObject.ApplyModifiedProperties(); // Apply changes immediately
                }

                EditorGUI.BeginDisabledGroup(true); // Disable the following fields
                EditorGUI.PropertyField(idRect, idProperty, new GUIContent("ID"));
                EditorGUI.PropertyField(secretKeyRect, secretKeyProperty, new GUIContent("Secret Key"));
                EditorGUI.EndDisabledGroup();

                // Draw Update button if changes were made
                if (hasChanges)
                {
                    if (GUI.Button(buttonRect, "Update Leaderboard"))
                    {
                        // Call UpdateLeaderboard method
                        ((HyplayLeaderboard)property.objectReferenceValue).UpdateLeaderboard();
                        hasChanges = false; // Reset flag after update
                    }
                }

                EditorGUI.EndProperty();
        }
        
        private void RefreshLeaderboardNames()
        {
            leaderboardNames = Resources.FindObjectsOfTypeAll<HyplayLeaderboard>()
                .Select(lb => lb.name)
                .OrderBy(lb => lb)
                .ToList();
            lastRefreshTime = EditorApplication.timeSinceStartup;
        }
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.serializedObject.targetObject is HyplayLeaderboards)
            {
                // Adjust height based on the number of fields and button visibility
                float height = EditorGUIUtility.singleLineHeight * 4 + 20; // 5 fields + spacing
                if (hasChanges)
                {
                    height += EditorGUIUtility.singleLineHeight + 10; // Add button height + spacing
                }

                return height;
            }
            
            return base.GetPropertyHeight(property, label);
        }
    }
}
using System;
using UnityEditor;
using UnityEngine;

namespace Solo.MOST_IN_ONE
{
    // Big Header Attribute
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class BigHeaderAttribute : PropertyAttribute
    {
        public string Text { get; } = string.Empty;
        public BigHeaderAttribute(string text) { Text = text; }
    }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(BigHeaderAttribute))]
    public class BigHeaderPropertyDrawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            BigHeaderAttribute attributeHandle = (BigHeaderAttribute)attribute;
            position.yMin += EditorGUIUtility.singleLineHeight * 0.5f;
            position = EditorGUI.IndentedRect(position);
            GUIStyle headerTextStyle = new()
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft
            };
            headerTextStyle.normal.textColor = new Color32(252, 191, 1, 255);
            GUI.Label(position, attributeHandle.Text, headerTextStyle);
            EditorGUI.DrawRect(new Rect(position.xMin, position.yMin, position.width, 1), new Color32(252, 191, 1, 255));
        }
        public override float GetHeight()
        {
            return EditorGUIUtility.singleLineHeight * 1.5f;
        }
    }
    #endif

    // ReadOnly Attribute
    public class ReadOnlyAttribute : PropertyAttribute { }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
    #endif
}

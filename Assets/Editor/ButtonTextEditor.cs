//using UnityEditor;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//public class ButtonTextEditor : EditorWindow
//{
//    private Button button;
//    private TMP_Text buttonText;
//    private string newText;
//    private int newFontSize;
//    private Color newColor;
//    private TMP_FontAsset newFont;
//    private FontStyles newFontStyle;
//    private TextAlignmentOptions newAlignment;
//    [MenuItem("Window/Button Text Editor")]
//    public static void ShowWindow()
//    {
//        GetWindow<ButtonTextEditor>("Button Text Editor");
//    }
//    private void OnGUI()
//    {
//        GUILayout.Label("Select a Button to Edit", EditorStyles.boldLabel);
//        button = (Button)EditorGUILayout.ObjectField("Button", button, typeof(Button), true);
//        if (button != null)
//        {
//            buttonText = button.GetComponentInChildren<TMP_Text>();
//            if (buttonText != null)
//            {
//                newText = EditorGUILayout.TextField("Text", buttonText.text);
//                newFontSize = EditorGUILayout.IntField("Font Size", buttonText.fontSize);
//                newColor = EditorGUILayout.ColorField("Color", buttonText.color);
//                newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Font", buttonText.font, typeof(TMP_FontAsset), true);
//                newFontStyle = (FontStyles)EditorGUILayout.EnumPopup("Font Style", buttonText.fontStyle);
//                newAlignment = (TextAlignmentOptions)EditorGUILayout.EnumPopup("Alignment", buttonText.alignment);
//                if (GUILayout.Button("Apply Changes"))
//                {
//                    ApplyChanges();
//                }
//            }
//            else
//            {
//                GUILayout.Label("Selected Button does not have a TextMeshProUGUI component as a child.", EditorStyles.helpBox);
//            }
//        }
//    }
//    private void ApplyChanges()
//    {
//        buttonText.text = newText;
//        buttonText.fontSize = newFontSize;
//        buttonText.color = newColor;
//        buttonText.font = newFont;
//        buttonText.fontStyle = newFontStyle;
//        buttonText.alignment = newAlignment;
//        EditorUtility.SetDirty(buttonText);
//    }
//}
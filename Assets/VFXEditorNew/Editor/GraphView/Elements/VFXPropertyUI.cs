﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    class VFXPropertyUI : VisualContainer
    {
        VFXBlockPresenter.PropertyInfo m_PropertyInfo;
        VFXBlockPresenter              m_Presenter;

        VFXPropertyIM                      m_Property;

        IMGUIContainer          m_Container;
        VisualContainer         m_Slot;
        VFXDataAnchor           m_SlotIcon;

        static int s_ContextCount = 1;


        GUIStyles m_GUIStyles = new GUIStyles();


        const string SelectedFieldBackgroundProperty = "selected-field-background";
        const string IMBorderProperty = "im-border";
        const string IMPaddingProperty = "im-padding";


        StyleProperty<Texture2D> m_SelectedFieldBackground;
        public Texture2D selectedFieldBackground
        {
            get
            {
                return m_SelectedFieldBackground.GetOrDefault(null);
            }
        }

        StyleProperty<int> m_IMBorder;
        public int IMBorder
        {
            get
            {
                return m_IMBorder.GetOrDefault(0);
            }
        }

        StyleProperty<int> m_IMPadding;
        public int IMPadding
        {
            get
            {
                return m_IMPadding.GetOrDefault(0);
            }
        }

        public override void OnStylesResolved(VisualElementStyles elementStyles)
        {
            base.OnStylesResolved(elementStyles);
            elementStyles.ApplyCustomProperty(SelectedFieldBackgroundProperty, ref m_SelectedFieldBackground);
            elementStyles.ApplyCustomProperty(IMBorderProperty, ref m_IMBorder);
            elementStyles.ApplyCustomProperty(IMPaddingProperty, ref m_IMPadding);

            m_GUIStyles.baseStyle.active.background = selectedFieldBackground;
            m_GUIStyles.baseStyle.focused.background = m_GUIStyles.baseStyle.active.background;

            m_GUIStyles.baseStyle.border.top = m_GUIStyles.baseStyle.border.left = m_GUIStyles.baseStyle.border.right = m_GUIStyles.baseStyle.border.bottom = IMBorder;
            m_GUIStyles.baseStyle.padding = new RectOffset(IMPadding, IMPadding, IMPadding, IMPadding);
        }


        VisualElement m_SpaceButton;

        public VFXPropertyUI()
        {
            m_Slot = new VisualContainer();
            m_Slot.AddToClassList("slot");
            AddChild(m_Slot);
            m_Slot.clipChildren = false;
            clipChildren = false;

            m_Container = new IMGUIContainer();
            m_Container.OnGUIHandler = OnGUI;
            m_Container.executionContext = s_ContextCount++;
            AddChild(m_Container);

            m_SpaceButton = new VisualElement();
            m_SpaceButton.AddManipulator(new Clickable(SwitchSpace));
            m_SpaceButton.AddToClassList("space");

            m_SpaceButton.content = new GUIContent();

            m_GUIStyles.baseStyle = new GUIStyle();
        }

        void SwitchSpace()
        {
            ((Spaceable)m_PropertyInfo.value).space = (CoordinateSpace) ((int)(((Spaceable)m_PropertyInfo.value).space + 1) % (int)CoordinateSpace.SpaceCount);
            m_Presenter.PropertyValueChanged(ref m_PropertyInfo);
        }

        public class GUIStyles
        {
            public GUIStyle baseStyle;

            public GUIStyle GetGUIStyleForExpandableType(Type type)
            {
                GUIStyle style = null;

                if (typeStyles.TryGetValue(type, out style))
                {
                    return style;
                }

                GUIStyle typeStyle = new GUIStyle(baseStyle);
                typeStyle.normal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_plus");
                if(typeStyle.normal.background == null)
                    typeStyle.normal.background = Resources.Load<Texture2D>("VFX/Default_plus");
                typeStyle.active.background = typeStyle.focused.background = null;
                typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/" + type.Name + "_minus");
                if (typeStyle.onNormal.background == null)
                    typeStyle.onNormal.background = Resources.Load<Texture2D>("VFX/Default_minus");
                typeStyle.border.top = 0;
                typeStyle.border.left = 0;
                typeStyle.border.bottom = typeStyle.border.right = 0;
                typeStyle.padding.top = 3;

                typeStyles.Add(type, typeStyle);


                return typeStyle;
            }

            public GUIStyle GetGUIStyleForType(Type type)
            {
                GUIStyle style = null;

                if (typeStyles.TryGetValue(type, out style))
                {
                    return style;
                }

                GUIStyle typeStyle = new GUIStyle(baseStyle);
                typeStyle.normal.background = Resources.Load<Texture2D>("VFX/" + type.Name);
                if (typeStyle.normal.background == null)
                    typeStyle.normal.background = Resources.Load<Texture2D>("VFX/Default");
                typeStyle.active.background = typeStyle.focused.background = null;
                typeStyle.border.top = 0;
                typeStyle.border.left = 0;
                typeStyle.border.bottom = typeStyle.border.right = 0;

                typeStyles.Add(type, typeStyle);


                return typeStyle;
            }

            Dictionary<Type, GUIStyle> typeStyles = new Dictionary<Type, GUIStyle>();

            public void Reset()
            {
                typeStyles.Clear();
            }

            public float lineHeight
            { get { return baseStyle.fontSize * 1.25f; } }
        }



        void OnGUI()
        {
            // update the GUISTyle from the element style defined in USS
            bool different = false;

            if( m_GUIStyles.baseStyle.font != font )
            {
                m_GUIStyles.baseStyle.font = font;
                different = true;
            }
            if (m_GUIStyles.baseStyle.fontSize != fontSize)
            {
                m_GUIStyles.baseStyle.fontSize = fontSize;
                different = true;
            }
            if (m_GUIStyles.baseStyle.focused.textColor != textColor)
            {
                m_GUIStyles.baseStyle.focused.textColor = m_GUIStyles.baseStyle.active.textColor = m_GUIStyles.baseStyle.normal.textColor = textColor;
                different = true;
            }

            if (different)
                m_GUIStyles.Reset();

            bool changed = m_Property.OnGUI(m_Presenter, ref m_PropertyInfo, m_GUIStyles);

            if( changed )
            {
                Dirty(ChangeType.Transform|ChangeType.Repaint);
            }

            if (Event.current.type != EventType.Layout && Event.current.type != EventType.Used)
            {
                Rect r = GUILayoutUtility.GetLastRect();
                m_Container.height = r.yMax;
            }
        }

        public void DataChanged(VFXBlockUI block,VFXBlockPresenter.PropertyInfo info)
        {
            var newPresenter = block.GetPresenter<VFXBlockPresenter>();

            if( newPresenter != m_Presenter)
            {
                if( m_Presenter != null)
                {
                    m_Presenter.OnParamChanged -= OnParamChanged;
                }
                if( newPresenter != null)
                {
                    newPresenter.OnParamChanged += OnParamChanged;
                }

                m_Presenter = newPresenter;
            }


            
            if( m_PropertyInfo.type != info.type)
            {
                m_Property = VFXPropertyIM.Create(info.type);
            }
            m_PropertyInfo = info;


            m_Presenter.OnParamChanged += OnParamChanged;


            VFXDataAnchorPresenter presenter = m_Presenter.GetPropertyPresenter(ref info);
            if (m_SlotIcon == null)
            {
                m_SlotIcon = VFXDataAnchor.Create<VFXDataEdgePresenter>(presenter);
                m_Slot.AddChild(m_SlotIcon);
            }
            else
            {
                m_SlotIcon.presenter = presenter;
                Dirty(ChangeType.Repaint|ChangeType.Transform);
            }

            if (typeof(Spaceable).IsAssignableFrom(info.type))
            {
                if (m_SpaceButton.parent == null)
                {
                    AddChild(m_SpaceButton);
                }

                CoordinateSpace space = ((Spaceable)info.value).space;
                m_SpaceButton.content.text = space.ToString();
                m_SpaceButton.name = space.ToString();

                foreach (string spaceName in Enum.GetNames(typeof(CoordinateSpace)))
                {
                    m_SpaceButton.RemoveFromClassList(spaceName.ToLower());
                }

                m_SpaceButton.AddToClassList(space.ToString().ToLower());
                m_SpaceButton.Dirty(ChangeType.Styles);
            }
            else
            {
                if (m_SpaceButton.parent != null)
                {
                    RemoveChild(m_SpaceButton);
                }
            }
            
        }

        private void OnParamChanged(VFXBlockPresenter blockPresenter)
        {
            //TODO update RMGUIControl
        }
    }
}

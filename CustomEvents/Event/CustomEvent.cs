using System;
using System.Reflection;
using ADOFAI;
using UnityEngine;

namespace CustomEvents.Event
{
    public abstract class CustomEvent
    {
        public Assembly Assembly
        {
            get
            {
                _type ??= GetType();

                return _type.Assembly;
            }
        }
        private Type _type;
        
        public string FullName
        {
            get
            {
                field ??= Assembly.GetName().Name;

                return $"{field}::{Name}";
            }
        }
        
        public virtual string Name
        {
            get
            {
                _type ??= GetType();
                field ??= _type.Name;

                return field;
            }
        }
        
        public virtual bool IsEnabled => true;
       
        public virtual bool StretchEditorViewport => false;
        public virtual bool IsDevOnly => false;
        public virtual bool NeedsDLC => false;
        public virtual bool AllowFirstFloor => false;
        public virtual bool IsDecoration => false;
        public virtual LevelEventExecutionTime ExecutionTime => LevelEventExecutionTime.OnBar;

        public virtual Sprite GetIcon()
        {
            return GCS.levelEventIcons == null ? Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero) : GCS.levelEventIcons[LevelEventType.Bookmark];
        }

        public virtual void OnApply()
        {
        }

        public virtual void OnFloor()
        {
        }
    }
}
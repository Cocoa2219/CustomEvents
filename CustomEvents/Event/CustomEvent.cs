using System;
using System.Reflection;
using ADOFAI;
using UnityEngine;

namespace CustomEvents.Event
{
    /// <summary>Provides a base skeletal class for defining extendable custom level event behavior and structural layout settings profiles.</summary>
    public abstract class CustomEvent
    {
        /// <summary>Gets the host <see cref="Assembly"/> metadata framework that declares this specific custom event type.</summary>
        /// <remarks>This property caches its reflectively derived type data locally upon its initial evaluation loop sequence.</remarks>
        public Assembly Assembly
        {
            get
            {
                _type ??= GetType();

                return _type.Assembly;
            }
        }
        private Type _type;
        
        /// <summary>Gets the unique, fully qualified structural identifier key used to register and map this event across serialization pipelines.</summary>
        /// <remarks>Combines the host assembly name with the class identity signature, formatting the entry string as <c>AssemblyName::EventName</c>.</remarks>
        public string FullName
        {
            get
            {
                field ??= Assembly.GetName().Name;

                return $"{field}::{Name}";
            }
        }
        
        /// <summary>Gets the human-readable string identifier used for editor labels, fallback translations, and indexing lookups.</summary>
        /// <remarks>Defaults to the concrete class name via reflection if an explicit override is not supplied.</remarks>
        public virtual string Name
        {
            get
            {
                _type ??= GetType();
                field ??= _type.Name;

                return field;
            }
        }
        
        /// <summary>Gets a value indicating whether this custom event is currently discovered, instantiated, and processed by active registration loops.</summary>
        public virtual bool IsEnabled => true;
       
        /// <summary>Gets a value indicating whether the level editor inspector workspace coordinates should expand dynamically when this element type assumes focus parameters.</summary>
        public virtual bool StretchEditorViewport => false;

        /// <summary>Gets a value indicating whether exposure permissions for this definition should be strictly constrained to active development environments.</summary>
        public virtual bool IsDevOnly => false;

        /// <summary>Gets a value indicating whether execution requires downloadable content authorization flags to be present inside the host runtime container environment.</summary>
        public virtual bool NeedsDLC => false;

        /// <summary>Gets a value indicating whether this custom event configuration can be legally placed on a level track's starting node index sequence.</summary>
        public virtual bool AllowFirstFloor => false;

        /// <summary>Gets a value indicating whether this event bypasses gameplay calculation tracks to operate purely as an unindexed visual element layout decoration layer.</summary>
        public virtual bool IsDecoration => false;

        /// <summary>Gets the target synchronization timeline index marker state where the runtime game loop triggers this instance's execution calculations.</summary>
        /// <remarks>Defaults out to <see cref="LevelEventExecutionTime.OnBar"/> synchronization behaviors.</remarks>
        public virtual LevelEventExecutionTime ExecutionTime => LevelEventExecutionTime.OnBar;

        /// <summary>Resolves the image asset descriptor used to illustrate this custom event entry block inside editor inventory tabs and layout grids rows panels.</summary>
        /// <returns>Returns a valid <see cref="Sprite"/> container, falling back onto a default <see cref="LevelEventType.Bookmark"/> texture asset if game asset tables are uninitialized.</returns>
        public virtual Sprite GetIcon()
        {
            return GCS.levelEventIcons == null ? Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero) : GCS.levelEventIcons[LevelEventType.Bookmark];
        }

        /// <summary>Triggers custom execution logic parameters hooks right when properties get deserialized, injected, or transformed within the editor level mapping workspace.</summary>
        protected internal virtual void OnApply()
        {
        }

        /// <summary>Triggers when a player's active planet steps over the floor linked to this specific event state instance.</summary>
        protected internal virtual void OnFloor()
        {
        }
    }
}
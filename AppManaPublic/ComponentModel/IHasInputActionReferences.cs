using System;
using AppMana.InteractionToolkit;
using AppManaPublic.Configuration;
using UnityEngine.InputSystem;

namespace AppMana.ComponentModel
{
    /// <summary>
    /// Used by <see cref="RemotePlayableConfiguration"/> to fix inputs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When using input actions, any <c>.performed +=</c> and <c>.canceled +=</c> handlers should be declared on the input
    /// actions asset actions retrieved from <c>RemotePlayableConfiguration.actions</c> asset property in a <c>Start</c> or later;
    /// all <c>InputActionReference</c> fields and properties should be declared in implementations of
    /// <c>IHasInputActionReferences</c>. AppMana replaces the input action map in <c>RemotePlayableConfiguration.Awake</c>, and
    /// undeclared input action references will not be enabled for streaming. 
    /// </para>
    /// <para>As an alternative, use <see cref="MultiplayerInputActionReference"/> fields instead, which will be
    /// patched automatically.</para>
    /// <example>
    /// Here's an example implementation of IHasInputActionReferences:
    /// <code>
    /// public class MyScript : MonoBehaviour, IHasInputActionReferences
    /// {
    ///     public InputActionReference someAction1;
    ///     public InputActionReference someAction2;
    ///
    ///     public IHasInputActionReferences.InputActionReferenceProperty[] inputActionReferenceProperties
    ///     {
    ///         get
    ///         {
    ///             return new[]
    ///             {
    ///                 new IHasInputActionReferences.InputActionReferenceProperty(
    ///                     () => someAction1,
    ///                     value => someAction1 = value),
    ///                 new(
    ///                     () => someAction2,
    ///                     value => someAction2 = value)
    ///             };
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <seealso cref="MultiplayerInputActionReference"/>
    public interface IHasInputActionReferences
    {
        public struct InputActionReferenceProperty
        {
            public Func<InputActionReference> get { get; set; }
            public Action<InputActionReference> set { get; set; }

            public InputActionReferenceProperty(Func<InputActionReference> get, Action<InputActionReference> set)
            {
                this.get = get;
                this.set = set;
            }
        }

        /// <summary>
        /// Returns an array of input action reference fields or properties on this object.
        /// </summary>
        InputActionReferenceProperty[] inputActionReferenceProperties { get; }
    }
}
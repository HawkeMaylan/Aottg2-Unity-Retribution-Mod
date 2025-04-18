﻿using System.Collections.Generic;
using UnityEngine;
using Map;

namespace CustomLogic
{
    class CustomLogicTransformBuiltin: CustomLogicBaseBuiltin
    {
        public Transform Value;
        private Vector3 _internalRotation;
        private Vector3 _internalLocalRotation;
        private bool _needSetRotation = true;
        private bool _needSetLocalRotation = true;
        private string _currentAnimation;
        private Dictionary<string, AnimationClip> _animatorClips;

        public CustomLogicTransformBuiltin(Transform transform): base("Transform")
        {
            Value = transform;
        }

        public override object CallMethod(string methodName, List<object> parameters)
        {
            if (methodName == "GetTransform")
            {
                string name = (string)parameters[0];
                Transform transform = Value.Find(name);
                if (transform != null)
                {
                    return new CustomLogicTransformBuiltin(transform);
                }
                return null;
            }
            if (methodName == "GetTransforms")
            {
                CustomLogicListBuiltin listBuiltin = new CustomLogicListBuiltin();
                foreach (Transform transform in Value)
                {
                    listBuiltin.List.Add(new CustomLogicTransformBuiltin(transform));
                }
                return listBuiltin;
            }
            if (methodName == "PlayAnimation")
            {
                string anim = (string)parameters[0];
                float fade = 0.1f;
                if (parameters.Count > 1)
                    fade = (float)parameters[1];
                var animation = Value.GetComponent<Animation>();
                if (animation != null)
                {
                    
                    if (!animation.IsPlaying(anim))
                        animation.CrossFade(anim, fade);
                    return null;
                }
                var animator = Value.GetComponent<Animator>();
                if (animator != null)
                {
                    anim = anim.Replace('.', '_');
                    if (_currentAnimation != anim)
                    {
                        animator.CrossFade(anim, fade);
                        _currentAnimation = anim;
                    }
                }
                return null;
            }
            if (methodName == "GetAnimationLength")
            {
                string anim = (string)parameters[0];
                var animation = Value.GetComponent<Animation>();
                if (animation != null)
                    return animation[anim].length;
                var animator = Value.GetComponent<Animator>();
                if (animator != null)
                {
                    anim = anim.Replace('.', '_');
                    if (_animatorClips == null)
                    {
                        _animatorClips = new Dictionary<string, AnimationClip>();
                        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
                            _animatorClips[clip.name.Replace('.', '_')] = clip;
                    }
                    return _animatorClips[anim].length;
                }
                return null;
            }
            if (methodName == "PlaySound")
            {
                var sound = Value.GetComponent<AudioSource>();
                if (!sound.isPlaying)
                    sound.Play();
                return null;
            }
            if (methodName == "StopSound")
            {
                var sound = Value.GetComponent<AudioSource>();
                if (sound.isPlaying)
                    sound.Stop();
                return null;
            }
            if (methodName == "ToggleParticle")
            {
                var particle = Value.GetComponent<ParticleSystem>();
                if (!particle.isPlaying)
                    particle.Play();
                var emission = particle.emission;
                emission.enabled = (bool)parameters[0];
                return null;
            }
            if (methodName == "InverseTransformDirection")
            {
                var direction = (CustomLogicVector3Builtin)parameters[0];
                return new CustomLogicVector3Builtin(Value.InverseTransformDirection(direction.Value));
            }
            if (methodName == "InverseTransformPoint")
            {
                var point = (CustomLogicVector3Builtin)parameters[0];
                return new CustomLogicVector3Builtin(Value.InverseTransformPoint(point.Value));
            }
            if (methodName == "TransformDirection")
            {
                var direction = (CustomLogicVector3Builtin)parameters[0];
                return new CustomLogicVector3Builtin(Value.TransformDirection(direction.Value));
            }
            if (methodName == "TransformPoint")
            {
                var point = (CustomLogicVector3Builtin)parameters[0];
                return new CustomLogicVector3Builtin(Value.TransformPoint(point.Value));
            }
            if (methodName == "Rotate")
            {
                var rotation = (CustomLogicVector3Builtin)parameters[0];
                Value.Rotate(rotation.Value);
                return null;
            }
            if (methodName == "RotateAround")
            {
                var point = (CustomLogicVector3Builtin)parameters[0];
                var axis = (CustomLogicVector3Builtin)parameters[1];
                var angle = (float)parameters[2];
                Value.RotateAround(point.Value, axis.Value, angle);
                return null;
            }
            if (methodName == "LookAt")
            {
                var target = (CustomLogicVector3Builtin)parameters[0];
                Value.LookAt(target.Value);
                return null;
            }
            if (methodName == "SetRenderersEnabled")
            {
                bool enabled = (bool)parameters[0];
                foreach (var renderer in Value.GetComponentsInChildren<Renderer>())
                    renderer.enabled = enabled;

                return null;
            }

            return base.CallMethod(methodName, parameters);
        }

        public override object GetField(string name)
        {
            if (name == "Position")
                return new CustomLogicVector3Builtin(Value.position);
            if (name == "LocalPosition")
                return new CustomLogicVector3Builtin(Value.localPosition);
            if (name == "Rotation")
            {
                if (_needSetRotation)
                {
                    _internalRotation = Value.rotation.eulerAngles;
                    _needSetRotation = false;
                }
                return new CustomLogicVector3Builtin(_internalRotation);
            }
            if (name == "LocalRotation")
            {
                if (_needSetLocalRotation)
                {
                    _internalLocalRotation = Value.localRotation.eulerAngles;
                    _needSetLocalRotation = false;
                }
                return new CustomLogicVector3Builtin(_internalLocalRotation);
            }
            if (name == "QuaternionRotation")
            {
                return new CustomLogicQuaternionBuiltin(Value.rotation);
            }
            if (name == "QuaternionLocalRotation")
            {
                return new CustomLogicQuaternionBuiltin(Value.localRotation);
            }
            if (name == "Scale")
            {
                var scale = Value.localScale;
                return new CustomLogicVector3Builtin(scale);
            }
            if (name == "Forward")
                return new CustomLogicVector3Builtin(Value.forward.normalized);
            if (name == "Up")
                return new CustomLogicVector3Builtin(Value.up.normalized);
            if (name == "Right")
                return new CustomLogicVector3Builtin(Value.right.normalized);
            return base.GetField(name);
        }

        public override void SetField(string name, object value)
        {
            if (name == "Position")
                Value.position = ((CustomLogicVector3Builtin)value).Value;
            else if (name == "LocalPosition")
                Value.localPosition = ((CustomLogicVector3Builtin)value).Value;
            else if (name == "Rotation")
            {
                _internalRotation = ((CustomLogicVector3Builtin)value).Value;
                _needSetRotation = false;
                Value.rotation = Quaternion.Euler(_internalRotation);
            }
            else if (name == "LocalRotation")
            {
                _internalLocalRotation = ((CustomLogicVector3Builtin)value).Value;
                _needSetLocalRotation = false;
                Value.localRotation = Quaternion.Euler(_internalLocalRotation);
            }
            else if (name == "QuaternionRotation")
            {
                Value.rotation = ((CustomLogicQuaternionBuiltin)value).Value;
            }
            else if (name == "QuaternionLocalRotation")
            {
                Value.localRotation = ((CustomLogicQuaternionBuiltin)value).Value;
            }
            else if (name == "Scale")
                Value.localScale = ((CustomLogicVector3Builtin)value).Value;
            else if (name == "Forward")
                Value.forward = ((CustomLogicVector3Builtin)value).Value;
            else if (name == "Up")
                Value.up = ((CustomLogicVector3Builtin)value).Value;
            else if (name == "Right")
                Value.right = ((CustomLogicVector3Builtin)value).Value;
            else
                base.SetField(name, value);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return Value == null;
            if (!(obj is CustomLogicTransformBuiltin))
                return false;
            return Value == ((CustomLogicTransformBuiltin)obj).Value;
        }
    }
}

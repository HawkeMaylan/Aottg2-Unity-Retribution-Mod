using UnityEngine;
using Photon.Pun;
using Characters;

namespace Characters
{
    class HumanMovementSync : BaseMovementSync
    {
        private Human _human;
        private int? _mountedParentViewID = null;
        private Vector3 _mountedPositionOffset = Vector3.zero;
        private Vector3 _mountedRotationOffset = Vector3.zero;

        protected override void Awake()
        {
            base.Awake();
            _human = GetComponent<Human>();
        }

        protected override void SendCustomStream(PhotonStream stream)
        {
            if (_human.MountState == HumanMountState.MapObject && _human.MountedTransform != null)
            {
                PhotonView mountedPV = _human.MountedTransform.GetComponent<PhotonView>();
                if (mountedPV != null)
                {
                    stream.SendNext(true); // IsMounted
                    stream.SendNext(mountedPV.ViewID);
                    stream.SendNext(_human.MountedPositionOffset);
                    stream.SendNext(_human.MountedRotationOffset);
                }
                else
                {
                    stream.SendNext(false); // Not mounted properly
                }
            }
            else
            {
                stream.SendNext(false); // Not mounted
            }

            //  ADD BACK: Head Rotation sync
            if (_human.LateUpdateHeadRotation.HasValue)
                stream.SendNext(_human.LateUpdateHeadRotation.Value);
            else
                stream.SendNext(null);
        }

        protected override void ReceiveCustomStream(PhotonStream stream)
        {
            bool isMounted = (bool)stream.ReceiveNext();
            if (isMounted)
            {
                _mountedParentViewID = (int)stream.ReceiveNext();
                _mountedPositionOffset = (Vector3)stream.ReceiveNext();
                _mountedRotationOffset = (Vector3)stream.ReceiveNext();
            }
            else
            {
                _mountedParentViewID = null;
            }

            //  ADD BACK: Head Rotation receive
            object receivedRotation = stream.ReceiveNext();
            if (receivedRotation is Quaternion q)
                _human.LateUpdateHeadRotationRecv = q;
            else
                _human.LateUpdateHeadRotationRecv = null;
        }

        protected override void Update()
        {
            if (!Disabled && !_photonView.IsMine)
            {
                if (_mountedParentViewID.HasValue)
                {
                    PhotonView mountedPV = PhotonView.Find(_mountedParentViewID.Value);
                    if (mountedPV != null)
                    {
                        _transform.position = mountedPV.transform.TransformPoint(_mountedPositionOffset);
                        _transform.rotation = Quaternion.Euler(mountedPV.transform.rotation.eulerAngles + _mountedRotationOffset);
                        return;
                    }
                }

                _transform.position = Vector3.Lerp(_transform.position, _correctPosition, Time.deltaTime * SmoothingDelay);
                _transform.rotation = Quaternion.Lerp(_transform.rotation, _correctRotation, Time.deltaTime * SmoothingDelay);

                if (_syncVelocity && _timeSinceLastMessage < MaxPredictionTime)
                {
                    _correctPosition += _correctVelocity * Time.deltaTime;
                    _timeSinceLastMessage += Time.deltaTime;
                }
            }
        }
    }
}

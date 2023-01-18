using KoboldKare;
using Photon.Pun;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Events;

public class GenericUsable : MonoBehaviourPun, ISavable, IPunObservable {
    public virtual Sprite GetSprite(Kobold k) { return null; }
    public virtual bool CanUse(Kobold k) { return true; }

    // Called only by us locally when the player tries to use an object. By default we try to inform everyone that we used it.
    public virtual void LocalUse(Kobold k) {
        photonView.RPC(nameof(GenericUsable.RPCUse), RpcTarget.All);
    }
    // Called globally by all clients, synced.
    public virtual void Use() { }

    // A passthrough to call from RPC
    [PunRPC]
    public void RPCUse() {
        PhotonProfiler.LogReceive(1);
        Use();
    }
    public virtual void Save(JSONNode node)
    {
        node["rotation.x"] = transform.eulerAngles.x;
        node["rotation.y"] = transform.eulerAngles.y;
        node["rotation.z"] = transform.eulerAngles.z;
    }
    public virtual void Load(JSONNode node) {
        Vector3 rotationCheck = new Vector3(node["rotation.x"], node["rotation.y"], node["rotation.z"]);
        if(rotationCheck != null && rotationCheck != Vector3.zero) { transform.eulerAngles = rotationCheck; }
    }
    public virtual void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
    }
}

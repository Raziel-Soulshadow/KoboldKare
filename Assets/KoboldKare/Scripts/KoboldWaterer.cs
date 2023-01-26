using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class KoboldWaterer : GenericWeapon {
    [SerializeField] private FluidStream stream;
    [SerializeField] private GenericReagentContainer container;
    private static readonly int Fire = Animator.StringToHash("Fire");

    [PunRPC]
    protected override void OnFireRPC(int viewID) {
        base.OnFireRPC(viewID);
        stream.OnFire(container);
    }

    [PunRPC]
    protected override void OnEndFireRPC(int viewID) {
        base.OnEndFireRPC(viewID);
        stream.OnEndFire();
    }
}

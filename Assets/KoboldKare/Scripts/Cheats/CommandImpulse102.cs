using System.Text;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

[System.Serializable]
public class CommandImpulse102 : Command {
    public override string GetArg0() => "/impulse102";
    public override void Execute(StringBuilder output, Kobold k, string[] args) {
        base.Execute(output, k, args);
        if (args.Length != 1) {
            throw new CheatsProcessor.CommandException("Usage: /impulse102");
        }
        if (!CheatsProcessor.GetCheatsEnabled()) {
            throw new CheatsProcessor.CommandException("Cheats are not enabled, use `/cheats 1` to enable cheats.");
        }

        k.photonView.RequestOwnership();
        KoboldGenes genes = k.GetGenes();
        k.photonView.RPC(nameof(Kobold.SetEnergyRPC), RpcTarget.All, genes.maxEnergy);
        output.Append("Energy Filled!\n");
    }
}

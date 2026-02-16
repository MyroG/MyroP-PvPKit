
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerTeleporter : UdonSharpBehaviour
{
	public Transform TeleportTo;

	public override void Interact()
	{
		Networking.LocalPlayer.TeleportTo(TeleportTo.position, TeleportTo.rotation);
	}
}

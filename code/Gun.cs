using Sandbox;
using Sandbox.Citizen;

public sealed class Gun : Component
{
	[Property] public CitizenAnimationHelper animationHelper;
	[Property] public GameObject handR;
	[Property] public CitizenAnimationHelper.HoldTypes holdType;
	[Property] public Model model;
	public GameObject WeaponObject;
	protected override void OnAwake()
	{
		base.OnAwake();
		WeaponObject = new();
		WeaponObject.Components.Create<ModelRenderer>().Model = model;
		WeaponObject.Components.Get<ModelRenderer>().RenderType = ModelRenderer.ShadowRenderType.Off;
		if (IsProxy)
		{
			WeaponObject.Parent = handR;
		}
	}
	protected override void OnPreRender()
	{
		if ( IsProxy )
		{
			Log.Info( WeaponObject );
			WeaponObject.Transform.Position = handR.Transform.Position;
			WeaponObject.Transform.Rotation = handR.Transform.Rotation;
			animationHelper.HoldType = holdType;
		}
	}
}

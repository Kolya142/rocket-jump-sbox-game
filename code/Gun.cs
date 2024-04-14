using Sandbox;
using Sandbox.Citizen;

public sealed class Gun : Component
{
	[Property] public CitizenAnimationHelper animationHelper;
	[Property] public GameObject handR;
	[Property] public CitizenAnimationHelper.HoldTypes holdType;
	[Property] public Model model;
	[Property] public bool isProxe;
	public GameObject WeaponObject;
	protected override void OnAwake()
	{
		base.OnAwake();
		WeaponObject = new();
		WeaponObject.Components.Create<ModelRenderer>().Model = model;
		WeaponObject.Components.Get<ModelRenderer>().RenderType = ModelRenderer.ShadowRenderType.Off;
		if ( !IsProxy ) 
			WeaponObject.Components.Get<ModelRenderer>().RenderType = ModelRenderer.ShadowRenderType.ShadowsOnly;
		WeaponObject.Parent = handR;
		isProxe = IsProxy;
	}
	protected override void OnPreRender()
	{
		// Log.Info( WeaponObject );
		WeaponObject.Transform.Position = handR.Transform.Position;
		WeaponObject.Transform.Rotation = handR.Transform.Rotation;
		animationHelper.HoldType = holdType;
	}
}

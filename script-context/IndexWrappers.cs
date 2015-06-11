﻿using System.Numerics;

namespace Yondr {

public static class IContextExtension {
	/// Creates a new entity with the given base.
	/// @param bass The name of the entity base to create the entity out of.
	/// @return The entity, or null if the base is invalid.
	public static Entity? CreateEntity(this IContext context, string group, string bass) {
		var index = context.CreateEntityI(group, bass);
		return index != null ? (Entity?)new Entity((EntityIdx)index, context) : null;
	}
}

public struct Entity {
	public Entity(EntityIdx index, IContext c) { Index = index; Context = c; }

	public Vector3 Position {
		get { return Context.EntityGetPosition(Index); }
		set { Context.EntitySetPosition(Index, value); }
	}
	public void Move(Vector3 amount) {
		Context.EntityMove(Index, amount);
	}

	public void LookAt(Vector3 position) {
		Context.EntityLookAt(Index, position);
	}
	public void RotateX(float radians) {
		Context.EntityRotateX(Index, radians);
	}
	public void RotateY(float radians) {
		Context.EntityRotateY(Index, radians);
	}
	public void RotateZ(float radians) {
		Context.EntityRotateZ(Index, radians);
	}
	public Vector3 Direction {
		get { return Context.EntityGetDirection(Index); }
	}

	public void SetAsCamera() {
		Context.SetCamera(Index);
	}

	public EntityIdx Index  { get; }
	public IContext Context { get; }
}

}

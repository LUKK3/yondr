﻿using NUnit.Framework;

[TestFixture]
public class PropertySystemTest {
	[Test]
	public void UseageTest() {
		var props = new PropertySystem();
		props.Add("fred", new Val(4.5));
		props.Add("paul", new Val("paulerson"));
		props.Add("carl", new Val(true));

		Assert.AreEqual(props.Count, 3);

		var fred = props.WithName("fred");
		var paul = props.WithName("paul");
		var carl = props.WithName("carl");

		Assert.AreNotEqual(fred.Index, paul.Index);
		Assert.AreNotEqual(fred.Index, carl.Index);
		Assert.AreEqual(paul.Value.AsString(), "paulerson");
		Assert.AreEqual(props.At(carl.Index).Name, "carl");
		Assert.AreEqual(props.WithName("phil"), null);
	}
}

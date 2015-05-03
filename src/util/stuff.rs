
use std::collections::HashMap;
use anymap::AnyMap;
use std::fmt::Debug;
use std::marker::Reflect;

/// Store anything with a string key.
pub struct Stuff {
	stuff: AnyMap,
}
impl Stuff {
	pub fn new() -> Stuff {
		Stuff { stuff: AnyMap::new() }
	}

	pub fn insert<V: 'static+Debug+Reflect>(&mut self, key: String, val: V) {
		match self.stuff.get_mut::<HashMap<String, V>>() {
			Some(map) => {
				if map.contains_key(&key) {
					warn!("Cat't store {:?}, since {:?} already exists in Stuff.", val, key);
				} else {
					map.insert(key, val);
				}
				return;
			},
			None => (),
		}
		let mut map: HashMap<String, V> = HashMap::new();
		map.insert(key, val);
		self.stuff.insert(map);
	}

	pub fn remove<V: 'static+Reflect>(&mut self, key: &str) -> Option<V> {
		match self.stuff.get_mut::<HashMap<String, V>>() {
			Some(map) => match map.remove(key) {
				Some(v) => Some(v),
				None => None,
			},
			None => None,
		}
	}

	pub fn get<V: 'static+Reflect>(&self, key: &str) -> Option<&V> {
		match self.stuff.get::<HashMap<String, V>>() {
			Some(map) => match map.get(key) {
				Some(v) => Some(v),
				None => None,
			},
			None => None,
		}
	}
}

#[cfg(test)]
mod test {
	use super::*;

	#[test]
	fn stuff() {
		let mut stuff = Stuff::new();
		stuff.insert("dogs", 5u32);
		stuff.insert("cats", "seven".to_string());
		assert_eq!(*stuff.get("dogs").unwrap(), 5u32);
		assert_eq!(*stuff.get("cats").unwrap(), "seven".to_string());
		assert_eq!(stuff.get("centaurs"), None::<&u32>);
		assert_eq!(stuff.remove("dogs").unwrap(), 5u32);
		assert_eq!(stuff.remove("dogs"), None::<u32>);
	}
}

import type { ContainerInterface } from "../../application";
import type { Factory, Registrations, TransientFactory } from "./registrations";

/** Typed DI container. Factories are lazy singletons unless wrapped with `Container.transient`. */
export default class Container<D> implements ContainerInterface<D> {
  private readonly instances = new Map<keyof D, unknown>();

  constructor(private readonly registrations: Registrations<D>) {}

  static transient<D, T>(factory: Factory<D, T>): TransientFactory<D, T> {
    return { transient: true, factory };
  }

  resolve<K extends keyof D>(key: K): D[K] {
    const registration = this.registrations[key];

    if (typeof registration !== "function") {
      return registration.factory(this);
    }

    if (!this.instances.has(key)) {
      this.instances.set(key, registration(this));
    }

    return this.instances.get(key) as D[K];
  }
}

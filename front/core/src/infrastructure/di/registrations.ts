import type { ContainerInterface } from "../../application";

export type Factory<D, T> = (container: ContainerInterface<D>) => T;

export type TransientFactory<D, T> = { readonly transient: true; readonly factory: Factory<D, T> };

/** One factory per key of `D`. A missing key or a wrong return type fails to compile. */
export type Registrations<D> = { [K in keyof D]: Factory<D, D[K]> | TransientFactory<D, D[K]> };

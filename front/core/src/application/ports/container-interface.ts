/** Resolves the dependencies declared in `D`. Resolving cannot fail: every key is registered at compile time. */
export default interface ContainerInterface<D> {
  resolve<K extends keyof D>(key: K): D[K];
}

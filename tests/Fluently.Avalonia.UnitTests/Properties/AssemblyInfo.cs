// The view locator falls back to the root provider of the most recently created navigation service,
// so test classes that each build their own container run one after another.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

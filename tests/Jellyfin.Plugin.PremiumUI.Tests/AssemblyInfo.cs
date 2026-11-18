using Xunit;

// El plugin expone una instancia estática (PremiumUiPlugin.Instance) y varios tests
// la reemplazan, así que la ejecución debe ser secuencial.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

using Microsoft.Extensions.DependencyInjection;
using System.Reflection;


namespace Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services)
        {
            //MediatR pipelines.
            var assembly = Assembly.GetExecutingAssembly();

            services
                .AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(assembly);
                });
            //.AddValidatorsFromAssembly(assembly)
            //.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachePipelineBehavior<,>));

            return services;
        }
    }
}

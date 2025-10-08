using GeneralReservationSystem.Application.Repositories.Interfaces;
using GeneralReservationSystem.Application.Repositories.Interfaces.Authentication;

namespace GeneralReservationSystem.Application.Repositories.Util.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        // NOTA IMPLEMENTATIVA: Se podría implementar un método genérico GetRepository<T>()
        // que devuelva el repositorio correspondiente según el tipo T, pero limita repositorios a IRepository<T>
        // y sus métodos. Otra alternativa es que los repostitorios tengan un constructor adicional
        // que tome un IUnitOfWork, además del normal con conexión y transacción, y de ahi obtener
        // su conexión y posible transacción (en ese caso asegurado), pero acopla demasiado ambas jerarquías
        // (ya que la cuestión de conexión a DB excede la interfáz de IUnitOfWork, es imponer algo implementativo).
        // Por ello, se opta por la forma explícita, que es más verbosa pero más clara y flexible. Afortunadamente, 
        // no hay demasiados repositorios en este proyecto.
        public IRoleRepository RoleRepository { get; }
        public IUserRepository UserRepository { get; }
        public ISessionRepository SessionRepository { get; }
        public IDestinationRepository DestinationRepository { get; }
        public IDriverRepository DriverRepository { get; }
        public IReservationRepository ReservationRepository { get; }
        public ISeatRepository SeatRepository { get; }
        public ITripRepository TripRepository { get; }
        public IVehicleModelRepository VehicleModelRepository { get; }
        public IVehicleRepository VehicleRepository { get; }

        void Commit();
        void Rollback();
    }
}

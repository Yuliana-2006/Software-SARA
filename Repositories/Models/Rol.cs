namespace Software_SARA.Repositories.Models
{
    public class Rol
    {
        public int IdRol { get; set; }
        public string NomRol { get; set; } = null!;
        public virtual ICollection<Usuario> Usuarios { get; set; } = new List<Usuario>();
    }
}
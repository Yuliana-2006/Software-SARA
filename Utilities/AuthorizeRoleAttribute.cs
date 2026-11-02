using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;

namespace Software_SARA.Utilities
{
    // Este atributo nos permitirá proteger los controladores
    public class AuthorizeRoleAttribute : ActionFilterAttribute
    {
        private readonly string[] _rolesPermitidos;

        // Recibe los roles permitidos (ej: "Administrador", "Tesorero")
        public AuthorizeRoleAttribute(params string[] rolesPermitidos)
        {
            _rolesPermitidos = rolesPermitidos;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Obtenemos el rol de la sesión
            var session = context.HttpContext.Session;
            var rolUsuario = session.GetString("UsuarioRol");

            // Si no hay sesión o el rol no está en la lista de permitidos
            if (string.IsNullOrEmpty(rolUsuario) || !_rolesPermitidos.Contains(rolUsuario))
            {
                // Redirigimos a una vista de Acceso Denegado
                context.Result = new RedirectToActionResult("AccesoDenegado", "Acceso", null);
            }

            base.OnActionExecuting(context);
        }
    }
}
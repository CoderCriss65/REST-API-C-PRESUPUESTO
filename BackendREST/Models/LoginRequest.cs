﻿namespace BackendREST.Models
{
    public class LoginRequest
    {
        public string NombreUsuario { get; set; } = string.Empty;
        public string Contrasena { get; set; } = string.Empty;
    }
}

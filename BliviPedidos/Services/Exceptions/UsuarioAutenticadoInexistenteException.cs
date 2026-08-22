namespace BliviPedidos.Services.Exceptions;

public sealed class UsuarioAutenticadoInexistenteException : Exception
{
    public UsuarioAutenticadoInexistenteException()
        : base("A sessão autenticada pertence a um usuário que não existe mais.")
    {
    }
}

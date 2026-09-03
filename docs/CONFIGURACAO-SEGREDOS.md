# Configuracoes sensiveis

O `appsettings.json` versionado contem apenas a estrutura das configuracoes, sem credenciais. Valores reais devem ser fornecidos por um dos mecanismos abaixo.

## Desenvolvimento local com User Secrets

Execute os comandos a partir da raiz do repositorio, substituindo os valores entre `<...>`:

```powershell
dotnet user-secrets set "ConnectionStrings:MySqlConnection" "<CONEXAO_MYSQL>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "EmailSettings:PrimaryDomain" "<SERVIDOR_SMTP>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "EmailSettings:PrimaryPort" "587" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "EmailSettings:UsernameEmail" "<USUARIO_SMTP>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "EmailSettings:UsernamePassword" "<SENHA_SMTP>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "EmailSettings:FromEmail" "<REMETENTE>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "EmailSettings:ToEmail" "<DESTINATARIO>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "EmailSettings:CcEmail" "<DESTINATARIO_COPIA>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "Syncfusion:LicenseKey" "<LICENCA>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "BootstrapAdmin:Email" "<EMAIL_ADMIN>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "BootstrapAdmin:Password" "<SENHA_FORTE>" --project .\BliviPedidos\BliviPedidos.csproj
```

Como alternativa local, copie `appsettings.Local.example.json` para `appsettings.Local.json` e preencha os valores. O arquivo local esta no `.gitignore` e nunca deve ser commitado.

## Producao na KingHost

Cadastre as configuracoes como variaveis de ambiente no painel ou no mecanismo disponibilizado pela hospedagem. O ASP.NET Core troca `:` por `__` nos nomes:

```text
ConnectionStrings__MySqlConnection
EmailSettings__PrimaryDomain
EmailSettings__PrimaryPort
EmailSettings__UsernameEmail
EmailSettings__UsernamePassword
EmailSettings__FromEmail
EmailSettings__ToEmail
EmailSettings__CcEmail
Syncfusion__LicenseKey
BootstrapAdmin__Email
BootstrapAdmin__Password
```

As chaves `BootstrapAdmin` identificam o administrador global. Elas devem permanecer configuradas no User Secrets durante o desenvolvimento e como variáveis de ambiente seguras em produção. Somente o usuário cujo e-mail corresponda a `BootstrapAdmin:Email` pode acessar o console global `/Admin`; `BootstrapAdmin:Password` é usada apenas para criar essa conta quando ela ainda não existe.

Se `BootstrapAdmin:Email` estiver ausente, a aplicação inicia normalmente e mantém o console global bloqueado. Se o e-mail estiver configurado e a conta já existir, a senha não precisa estar presente durante as inicializações seguintes. A senha é obrigatória somente na primeira criação da conta; caso esteja ausente nesse momento, a aplicação também inicia, registra o problema no log e mantém o console global bloqueado.

O arquivo de User Secrets é carregado de forma opcional em todos os ambientes para permitir a execução local com `ASPNETCORE_ENVIRONMENT=Production`. No servidor publicado, prefira `BootstrapAdmin__Email` e `BootstrapAdmin__Password` como variáveis de ambiente, pois o processo do IIS normalmente é executado por uma conta diferente daquela que possui o `secrets.json` do desenvolvedor.

Variaveis de ambiente possuem prioridade sobre `appsettings.Local.json`, User Secrets e `appsettings.json`.

## Credenciais anteriormente versionadas

Remover uma credencial do arquivo atual nao a remove do historico do Git. Senhas de banco, SMTP, FTP e licencas que ja foram commitadas devem ser substituidas nos respectivos provedores. A limpeza do historico pode ser realizada separadamente depois da rotacao.

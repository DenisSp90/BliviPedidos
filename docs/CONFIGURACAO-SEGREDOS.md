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
dotnet user-secrets set "PixAppSettings:Responsavel" "<RESPONSAVEL>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "PixAppSettings:PixTipo" "<TIPO>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "PixAppSettings:PixChave" "<CHAVE_PIX>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "PixAppSettings:PixCity" "<CIDADE>" --project .\BliviPedidos\BliviPedidos.csproj
dotnet user-secrets set "Syncfusion:LicenseKey" "<LICENCA>" --project .\BliviPedidos\BliviPedidos.csproj
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
PixAppSettings__Responsavel
PixAppSettings__PixTipo
PixAppSettings__PixChave
PixAppSettings__PixCity
Syncfusion__LicenseKey
```

Variaveis de ambiente possuem prioridade sobre `appsettings.Local.json`, User Secrets e `appsettings.json`.

## Credenciais anteriormente versionadas

Remover uma credencial do arquivo atual nao a remove do historico do Git. Senhas de banco, SMTP, FTP e licencas que ja foram commitadas devem ser substituidas nos respectivos provedores. A limpeza do historico pode ser realizada separadamente depois da rotacao.

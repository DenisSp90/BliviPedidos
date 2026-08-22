# Plano de evolucao: BliviPedidos + loja virtual

## Objetivo

Evoluir o BliviPedidos para atender dois contextos na mesma aplicacao:

- operacao interna: produtos, estoque, pedidos, clientes e relatorios;
- loja virtual: catalogo, carrinho, checkout e acompanhamento pelo consumidor.

O BliviPedidos sera a fonte unica das regras de pedido e estoque. Cada cliente
comercial sera representado por uma `Loja`, sem copiar o projeto para cada cliente.

## Decisoes de arquitetura

1. Comecar como monolito modular ASP.NET Core MVC.
2. Separar as interfaces em `Areas/Admin` e `Areas/Loja`.
3. Manter um banco inicialmente, usando `LojaId` para isolamento.
4. Manter regras de negocio em servicos, sem duplica-las nos controllers.
5. Expor API publica somente por DTOs especificos, nunca pelas entidades do EF.
6. Permitir um frontend externo no futuro, consumindo a mesma API.
7. Tratar pedido e baixa de estoque em uma unica transacao atomica.

## Fase 0 - estabilizacao e seguranca

- [x] Atualizar o projeto para uma versao suportada do .NET.
- Decisao temporaria de hospedagem: usar .NET 9 por compatibilidade com a KingHost.
  Revisar e migrar para .NET 10 antes do fim do suporte do .NET 9, em novembro de 2026.
- [x] Proteger os endpoints administrativos da API.
- [x] Remover dados internos dos retornos publicos, especialmente `PrecoPago`.
- [x] Adicionar logs para falhas de pedido e movimentacao de estoque.
- [x] Separar configuracoes sensiveis do `appsettings.json` versionado.
- [x] Criar testes para conclusao e cancelamento de pedido.

Conclusao: dados administrativos nao podem ser obtidos sem autorizacao adequada.

## Fase 1 - fundacao multi-loja

Durante o desenvolvimento, o banco e descartavel e o esquema atual e criado com
`EnsureCreated`. Antes da publicacao definitiva, gerar uma nova migration inicial
consolidada e substituir `EnsureCreated` por `Migrate`.

- [x] Criar a entidade `Loja`.
- [x] Relacionar `Loja` com produto, categoria, cliente e pedido.
- [x] Criar uma loja padrao para os dados existentes.
- [x] Migrar os registros existentes para a loja padrao.
- [x] Tornar `LojaId` obrigatorio depois da migracao dos dados.
- [x] Associar usuarios internos a uma loja.
- [x] Resolver a loja atual por rota, dominio ou usuario.
- [x] Garantir filtro por `LojaId` em todas as consultas.
- [x] Criar uma tela para criar e gerenciar lojas, com slug, logo e cores.
Conclusao: uma loja nao consegue consultar nem alterar dados de outra loja.

## Fase 2 - area administrativa

- [x] Criar `Areas/Admin` e `_LayoutAdmin.cshtml`.
- [x] Mover gradualmente controllers e views administrativas (primeiro lote: gerenciamento de lojas).
- [x] Criar papeis `Administrador`, `Vendedor` e `Estoquista`.
- [x] Aplicar politicas de autorizacao por operacao.
- [x] Preservar redirecionamentos temporarios para URLs antigas.

Conclusao: funcoes internas ficam sob `/admin` e exigem o papel correto.

## Fase 3 - catalogo publico

- [x] Criar `Areas/Loja` e `_LayoutLoja.cshtml`.
- [x] Resolver a loja pelo `slug`, por exemplo `/loja/mkstore`.
- [x] Exibir somente produtos ativos e disponiveis daquela loja.
- [x] Criar busca, categorias e detalhes do produto.
- [x] Criar DTOs sem custo, movimentacoes ou dados internos.
- [x] Permitir logo, cores e informacoes configuraveis por loja.

Conclusao: o consumidor navega em um catalogo isolado sem acessar o administrativo.

## Fase 4 - carrinho e checkout

- [ ] Criar um carrinho separado do pedido definitivo.
- [ ] Manter carrinho anonimo por cookie/sessao com identificador seguro.
- [ ] Recalcular precos no servidor; nunca confiar no preco do navegador.
- [ ] Validar quantidades e produtos no servidor.
- [ ] Coletar ou selecionar os dados do consumidor.
- [ ] Criar o pedido somente ao confirmar o checkout.
- [ ] Gerar codigo publico nao sequencial para acompanhamento.
- [ ] Proteger formularios e limitar requisicoes abusivas.

Conclusao: o visitante confirma um pedido sem poder manipular preco ou estoque.

## Fase 5 - estoque e ciclo do pedido

- [ ] Substituir `Ativo` por um status de pedido explicito.
- [ ] Separar status do pedido de status do pagamento.
- [ ] Confirmar pedido e reservar/baixar estoque na mesma transacao.
- [ ] Implementar controle de concorrencia para a ultima unidade.
- [ ] Restaurar estoque de forma idempotente ao cancelar.
- [ ] Registrar ator, origem, pedido e loja em cada movimentacao.
- [ ] Definir expiracao de reservas nao pagas, caso sejam usadas.

Estados sugeridos: `Carrinho`, `AguardandoPagamento`, `Confirmado`,
`EmPreparacao`, `Pronto`, `Enviado`, `Concluido` e `Cancelado`.

Conclusao: concorrencia nao deixa estoque negativo e cancelamento nao devolve duas vezes.

## Fase 6 - consumidor e pagamentos

- [ ] Decidir entre visitante, conta opcional ou conta obrigatoria.
- [ ] Permitir ao consumidor consultar somente os proprios pedidos.
- [ ] Implementar confirmacao de e-mail/telefone quando necessaria.
- [ ] Integrar pagamento por uma interface propria.
- [ ] Validar webhooks com assinatura e idempotencia.
- [ ] Nunca aceitar do navegador a confirmacao de pagamento.

## Fase 7 - testes e publicacao

- [ ] Testes unitarios de pedido, estoque e isolamento por loja.
- [ ] Testes de integracao dos endpoints e autorizacao.
- [ ] Teste concorrente para compra da ultima unidade.
- [ ] Politica de backup e restauracao.
- [ ] Observabilidade, auditoria e alertas.
- [ ] Privacidade e retencao de dados pessoais.
- [ ] Pipeline de homologacao e producao.

## Ordem dos primeiros incrementos

1. Entidade `Loja` e migracao compativel com dados existentes.
2. Resolucao da loja atual e isolamento das consultas.
3. Protecao da API administrativa.
4. Area publica com catalogo somente leitura.
5. Carrinho separado do pedido.
6. Checkout transacional com controle de concorrencia.
7. Painel administrativo sob `Areas/Admin`.
8. Conta do consumidor e pagamento.

## Regra para o MkStore

Na primeira versao, o MkStore sera referencia visual e sua interface sera incorporada
na `Area Loja`. Se voltar a ser independente, consumira a API do BliviPedidos e nao
acessara diretamente o banco nem duplicara a regra de estoque.

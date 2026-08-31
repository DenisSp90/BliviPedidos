# Plano de retirada, entrega e cobranca por consumo

## Objetivo

Permitir que cada loja ofereca retirada, entrega propria ou ambas. Para pedidos
com entrega, o sistema calcula a distancia rodoviaria entre a loja e o endereco
do consumidor e aplica a tabela de frete da loja. A loja vende e recebe pela
entrega, administra seus entregadores e responde pela execucao. A plataforma
funciona como um balcao online e cobra da loja pelo consumo do recurso.

## Decisoes recomendadas

1. O consumidor escolhe `Retirada` ou `Entrega` antes de confirmar o pedido.
2. O frete e calculado pela rota rodoviaria, nunca pela distancia em linha reta.
3. A loja configura o endereco de origem e suas faixas de preco por quilometro.
4. O vendedor gerencia as regras e as entregas, mas o servidor calcula e congela
   os valores quando o pedido e confirmado.
5. Todo o valor do frete pertence a loja. A plataforma nao recebe nem repassa o
   pagamento da entrega.
6. A plataforma registra uma tarifa de uso para cada entrega computavel. Quanto
   mais entregas a loja realiza, maior e seu consumo faturavel.
7. O pedido guarda uma fotografia do calculo. Mudancas futuras na tabela de
   frete ou na tarifa de uso nao alteram pedidos ja computados.
8. O MVP nao precisa otimizar varias entregas em uma unica rota. Primeiro deve
   calcular loja -> consumidor; roteirizacao em lote pode vir depois.

## Experiencia do consumidor

### Retirada

- Exibir o endereco e, futuramente, o horario de retirada da loja.
- Nao exigir endereco de entrega.
- Frete, distancia e consumo faturavel de entrega iguais a zero.
- Fluxo sugerido: `Confirmado -> EmPreparacao -> Pronto -> Concluido`.

### Entrega

- Exigir endereco completo, inclusive numero, CEP, cidade e UF.
- Validar/geocodificar o endereco antes de apresentar o frete.
- Mostrar distancia, frete, subtotal dos produtos e total final antes da
  confirmacao.
- Se estiver fora da area atendida, impedir a confirmacao e explicar o motivo.
- Fluxo sugerido: `Confirmado -> EmPreparacao -> Pronto -> EmEntrega -> Concluido`.

O enum atual possui `Enviado`. No curto prazo ele pode ser exibido como "Saiu
para entrega"; no codigo, e preferivel renomear ou introduzir `EmEntrega` numa
migracao controlada para deixar o dominio claro.

## Configuracao da loja

Separar duas responsabilidades:

### Administrador da plataforma

- define o modelo e o valor da tarifa de uso por entrega de cada loja;
- ativa ou suspende o recurso de entrega da loja;
- consulta o extrato de consumo e o faturamento consolidado.

A tarifa deve ficar visivel para o vendedor, mas somente o administrador da
plataforma deve altera-la. O vendedor nao configura a cobranca que sera feita
contra a propria loja.

### Vendedor da loja

- habilita retirada e/ou entrega;
- cadastra o endereco de origem e confirma sua posicao no mapa;
- informa distancia maxima atendida;
- cadastra e ordena as faixas de preco;
- define pedido minimo, se aplicavel;
- cadastra entregadores e atribui pedidos;
- acompanha e conclui as entregas.

Uma loja deve ter ao menos uma modalidade ativa. A entrega so pode ser ativada
se houver origem valida e ao menos uma faixa de preco.

## Tabela de frete

### Modelo recomendado para o MVP: faixas com valor fixo

Exemplo:

| De (km) | Ate (km) | Frete |
|---:|---:|---:|
| 0,00 | 3,00 | R$ 7,00 |
| 3,01 | 6,00 | R$ 10,00 |
| 6,01 | 10,00 | R$ 15,00 |

Esse modelo e simples para a loja explicar e para o consumidor conferir. As
faixas nao podem se sobrepor nem deixar lacunas dentro da area atendida.

E possivel preparar o modelo para uma evolucao com `ValorBase + ValorPorKm`, mas
nao e recomendavel oferecer os dois modos na primeira versao.

### Regras de calculo

- Usar a distancia retornada pelo provedor em metros.
- Comparar as faixas em metros ou em decimal sem arredondar prematuramente.
- Exibir a distancia arredondada para duas casas apenas na interface.
- Nao aceitar frete negativo.
- Se nenhuma faixa cobrir a distancia, considerar fora da area de entrega.
- O total e `SubtotalProdutos + ValorFrete`.
- O frete e receita integral da loja e nao deve ser dividido com a plataforma.

## Cobranca da plataforma por consumo

O modelo escolhido e uma tarifa percentual individual por loja, aplicada sobre
a soma dos fretes das entregas concluidas. A referencia inicial e 2%, mas o
percentual fica definido no cadastro/contrato de cada loja.

Exemplo: uma loja concluiu 37 entregas e recebeu R$ 1.000,00 em fretes durante a
competencia. O demonstrativo registra quantidade 37, base de calculo de
R$ 1.000,00, tarifa de 2% e consumo faturavel de R$ 20,00. A quantidade e
informativa e comprova o consumo; ela nao multiplica novamente o percentual.

Essa tarifa e uma metrica de consumo contratual do software. A loja continua
recebendo integralmente o frete do consumidor; a plataforma nao retem nem repassa
parte desse pagamento. Cada loja pode ter sua propria tarifa comercial. A
estrutura pode aceitar futuramente tarifas progressivas por volume, sem alterar
os pedidos:

| Volume no periodo | Percentual de exemplo |
|---:|---:|
| 1 a 100 entregas | 2,00% |
| 101 a 500 entregas | 1,80% |
| acima de 500 | 1,60% |

Faixas por volume ficam apenas como evolucao. No MVP, usar o percentual individual
unico cadastrado para a loja durante toda a vigencia contratual.

Uma entrega deve ser computada somente ao chegar a `Concluido`. Retirada,
pedido cancelado e tentativa de entrega sem conclusao nao geram consumo, salvo
se os termos comerciais definirem outra regra. Reprocessamentos devem ser
idempotentes para nunca cobrar duas vezes a mesma entrega.

A assinatura tem vigencia/competencia de 12 meses, com datas explicitas de inicio
e fim no cadastro da loja. O consumo do periodo deve ser somado a cobranca da
assinatura, com linhas separadas: assinatura base, quantidade de entregas
concluidas, soma dos fretes, percentual aplicado e total do consumo. O painel
deve apresentar parciais mensais mesmo que o fechamento contratual seja anual.
Nao ha carteira de creditos nem uma segunda cobranca independente no MVP.

Recomendacao financeira: embora a vigencia seja anual, permitir faturamento
mensal do consumo ou cobranca proporcional no cancelamento da assinatura. Deixar
todo o consumo para receber apenas no fim de 12 meses aumenta inadimplencia e
dificulta a conciliacao. Se a escolha permanecer por fechamento anual, gerar
extrato mensal aceito/visivel pela loja e congelar os eventos de cada mes.

### Conclusao marcada por engano

Nao apagar o evento nem alterar diretamente o lancamento original. Disponibilizar
uma acao administrativa `Corrigir conclusao`, com:

- permissao restrita ao vendedor responsavel ou administrador;
- motivo obrigatorio;
- registro de usuario, data, status anterior e novo;
- retorno do pedido ao status operacional adequado;
- um lancamento compensatorio negativo (credito) vinculado ao lancamento
  original, se a competencia ainda estiver aberta;
- se a fatura ja estiver fechada, credito automatico na proxima fatura;
- bloqueio contra mais de uma compensacao para a mesma entrega.

Assim, a loja nao paga por uma entrega nao realizada e a plataforma preserva a
trilha de auditoria. Se depois a entrega for realmente concluida, uma nova
conclusao gera um novo lancamento de consumo, ligado ao mesmo historico.

Prazo recomendado: 7 dias corridos a partir da conclusao para o vendedor
gerencial fazer a correcao diretamente. Depois desse prazo, a correcao exige
aprovacao do administrador da plataforma, preservando motivo e evidencias. Se o
fechamento ja tiver sido faturado, o credito entra no proximo fechamento.

## Cancelamento depois da saida para entrega

Depois do status `EmEntrega`, o consumidor nao deve cancelar automaticamente.
Ele envia uma solicitacao, e o vendedor gerencial da loja decide conforme a
politica comercial exibida no checkout. Fluxo recomendado:

1. registrar `CancelamentoSolicitado`, sem apagar a rota nem a atribuicao;
2. avisar o vendedor e impedir mudancas concorrentes no pedido;
3. o vendedor escolhe continuar, cancelar ou registrar `TentativaSemSucesso`;
4. registrar motivo, horario, usuario e eventual valor que a propria loja decidiu
   cobrar ou devolver;
5. a plataforma nao decide reembolso e nao negocia com consumidor/entregador;
6. para a tarifa de software, somente `Concluido` entra na base de 2%; tentativa
   sem sucesso e cancelamento nao entram no MVP.

A loja deve publicar sua politica antes da confirmacao. Situacoes legais como
direito de arrependimento, produto perecivel, defeito ou atraso precisam ser
tratadas nos termos da loja e validadas juridicamente; uma regra do sistema nao
elimina direitos obrigatorios.

## Provedor de mapas recomendado para o MVP

Usar openrouteservice no plano gratuito, baseado em dados do OpenStreetMap, com:

- Geocoding para converter o endereco em coordenadas;
- Directions API com perfil de automovel (`driving-car`);
- retorno de distancia, duracao e, somente quando necessario, geometria da rota;
- uma interface interna `IRotaService`, mantendo a possibilidade de trocar o
  fornecedor.

Na consulta realizada em agosto de 2026, o plano Standard gratuito informa ate
2.000 requisicoes de Directions por dia (40 por minuto) e 1.000 requisicoes de
Geocoding por dia (100 por minuto). Limites e termos podem mudar; conferir o
painel da conta e as paginas oficiais antes de publicar:

- https://openrouteservice.org/services/
- https://openrouteservice.org/restrictions/

Controles iniciais sugeridos:

- aplicar limites internos abaixo da cota oficial e alertar em 70%, 90% e 100%
  do consumo diario;
- guardar a chave somente no servidor e fora do repositorio;
- armazenar coordenadas validadas da loja e evitar geocodifica-la a cada pedido;
- reutilizar uma cotacao durante sua validade e impedir recalculos por cliques
  repetidos;
- registrar custo, latencia e falhas por provedor;
- nao depender da instancia publica do Nominatim nem do servidor demonstrativo
  do OSRM em producao;
- se a cota acabar ou o servico ficar indisponivel, suspender novas cotacoes e
  mostrar uma mensagem clara; nao estimar distancia em linha reta;
- preparar migracao futura para plano contratado ou instancia propria de
  openrouteservice/OSRM se o volume superar o gratuito.

## Calculo da rota

Criar uma abstracao `IRotaService`, para que o dominio nao dependa diretamente
de Google Maps, Mapbox, HERE ou outro fornecedor. Ela deve receber origem e
destino geocodificados e retornar:

- distancia em metros;
- duracao estimada em segundos;
- identificador/nome do provedor;
- data e hora do calculo;
- opcionalmente a geometria da rota para exibicao futura.

Fluxo:

1. Geocodificar e salvar latitude/longitude da loja quando seu endereco mudar.
2. Geocodificar o destino informado no checkout.
3. Solicitar a rota em modo automovel (`DRIVE`) ao provedor no MVP.
4. Encontrar a faixa aplicavel e exibir a cotacao.
5. Gerar um token/identificador de cotacao de curta validade.
6. Ao confirmar, recalcular ou validar a cotacao no servidor e salvar a
   fotografia no pedido.

Se o provedor estiver indisponivel, nao inventar distancia. O MVP deve informar
que nao foi possivel cotar. Como evolucao, a loja pode receber o pedido como
"frete a confirmar", mas isso muda o contrato do checkout e deve ser uma opcao
explicita.

Um ajuste manual pelo vendedor pode ser oferecido somente depois da criacao do
pedido, com permissao especifica, motivo obrigatorio e registro dos valores
anterior e novo. Se o consumidor ja pagou, a diferenca exige um fluxo de cobranca
ou estorno; por isso, ajustes devem ser excecao.

## Modelo de dados proposto

### Loja

Adicionar configuracoes de alto nivel:

- `RetiradaAtiva`
- `EntregaAtiva`
- `EnderecoOrigem`, `NumeroOrigem`, `ComplementoOrigem`, `BairroOrigem`,
  `MunicipioOrigem`, `UfOrigem`, `CepOrigem`
- `LatitudeOrigem`, `LongitudeOrigem`
- `DistanciaMaximaEntregaKm` (opcional se as faixas ja determinarem o limite)
- `PedidoMinimoEntrega` (opcional)
- `PercentualConsumoEntrega` (inicialmente 2%, individual por loja)
- `AssinaturaInicioEm`, `AssinaturaTerminoEm` (vigencia de 12 meses)

Usar uma entidade separada com vigencia e historico para que mudancas de tarifa
nao alterem competencias anteriores.

### FaixaFreteLoja

- `Id`, `LojaId`
- `DistanciaInicialKm`, `DistanciaFinalKm`
- `ValorFrete`
- `Ativa`, `Ordem`
- datas de criacao e alteracao

Aplicar filtro por `LojaId`, como ja ocorre com pedidos, clientes e produtos.

### Pedido

- `ModalidadeAtendimento`: `Retirada` ou `Entrega`
- `SubtotalProdutos`
- `ValorFrete`
- `ValorTotalPedido`
- `DistanciaEntregaMetros`
- `DuracaoEstimadaSegundos`
- `ProvedorRota`
- `RotaCalculadaEm`
- `EntregadorId` (opcional)

O endereco presente em `Cadastro` ja funciona como fotografia do destinatario,
mas falta o numero. Ele deve ser adicionado e exigido apenas para entrega. Nao
usar o endereco atual do cadastro do cliente para reconstituir um pedido antigo.

### Entregador

Cadastro basico, pertencente a uma loja:

- `Id`, `LojaId`
- `Nome`
- `Telefone`
- `Documento` opcional, protegido e com acesso restrito
- `TipoVeiculo`: bicicleta, moto, carro ou outro
- `Placa` opcional
- `Ativo`
- contato de emergencia opcional
- observacao interna opcional

Para o MVP, o entregador nao precisa ser usuario do sistema nem ter aplicativo.
Ele e um recurso operacional atribuido ao pedido. Numa fase posterior, uma conta
propria pode permitir aceitar entrega, atualizar status e enviar localizacao.

### HistoricoEntrega

Registrar eventos em vez de depender apenas do status atual:

- `PedidoId`, `LojaId`, `EntregadorId` opcional
- `StatusAnterior`, `StatusNovo`
- `DataHora`, `UsuarioResponsavel`
- `Observacao`

Isso fornece auditoria de atribuicao, saida, conclusao, cancelamento e ajustes.

### LancamentoConsumoEntrega

Criar um lancamento imutavel de uso ajuda a fechar competencias e evitar
recalculos ou cobranca duplicada:

- `Id`, `LojaId`, `PedidoId`
- `Quantidade` (normalmente 1)
- `BaseCalculoFrete`
- `PercentualConsumoAplicado`
- `ValorTotal` (`BaseCalculoFrete * PercentualConsumoAplicado / 100`)
- `Situacao`: registrado, faturado, cancelado
- `Competencia`, `FaturadoEm`
- `EventoEntregaId` com chave unica para garantir idempotencia (um mesmo evento
  de conclusao nao pode gerar dois lancamentos)
- `LancamentoOrigemId` opcional para estorno/credito compensatorio
- `Tipo`: consumo ou credito

Criar o lancamento quando a entrega for concluida. O lancamento representa
consumo do software pela loja, nao comissao, repasse de frete ou pagamento ao
entregador. Se uma conclusao for desfeita por erro operacional, o ajuste deve ser
auditado em vez de apagar o registro.

## Telas necessarias

### Administracao da plataforma

- configuracao do plano/tarifa de consumo da loja;
- relatorio por loja, competencia e situacao;
- quantidade de entregas computadas, tarifa aplicada e total a faturar.

### Painel do vendedor

- configuracao de modalidades, origem, limite e faixas;
- lista e formulario de entregadores;
- fila de pedidos para entrega;
- atribuicao/troca de entregador;
- acao "saiu para entrega" e "entrega concluida";
- historico do pedido e motivo de eventual ajuste.

### Loja publica

- seletor claro `Retirar na loja` / `Receber no endereco`;
- campos de endereco condicionais;
- botao para calcular frete;
- resumo com produtos, frete e total;
- prazo estimado apresentado como estimativa, nao garantia.

## Regras de seguranca e consistencia

- Todo calculo financeiro deve ser repetido no servidor; nunca confiar no valor
  enviado pelo navegador.
- Todas as novas entidades operacionais devem respeitar o isolamento por loja.
- Um entregador so pode ser atribuido a pedido da mesma loja.
- Retirada nao aceita entregador, frete ou lancamento de consumo de entrega.
- Entrega exige endereco completo e uma cotacao valida.
- Valores de frete sao imutaveis depois da confirmacao e lancamentos de consumo
  sao imutaveis depois da conclusao, salvo fluxo auditado de ajuste.
- Chaves do provedor de mapas ficam fora do repositorio e, se possivel, a chamada
  de rota ocorre no servidor.
- Documento de entregador deve ser opcional no MVP; coletar apenas o necessario
  e aplicar controles compativeis com a LGPD.
- Interfaces e termos devem informar que a entrega e oferecida e executada pela
  loja. A plataforma fornece tecnologia de apoio. Essa separacao de produto nao
  substitui revisao juridica dos termos, contratos e operacao real.

## Entrega incremental

### Fase 1 - dominio e configuracao

- criar modalidades, endereco de origem, faixas de frete e plano de consumo;
- criar interfaces de servico de rota e calculo de frete;
- adicionar validacoes, isolamento por loja e testes unitarios;
- ainda sem alterar o checkout publico.

### Fase 2 - cotacao e checkout

- incluir escolha retirada/entrega;
- geocodificar destino, calcular rota e selecionar faixa;
- salvar fotografia financeira e geografica no pedido;
- incluir frete no PIX e em todas as exibicoes/relatorios do total.

### Fase 3 - operacao da entrega

- cadastrar entregadores;
- atribuir entregador e acompanhar estados;
- registrar historico auditavel;
- adaptar listas do vendedor e acompanhamento do consumidor.

### Fase 4 - consumo e faturamento da plataforma

- gerar um lancamento de consumo por entrega concluida;
- tratar conclusao, correcao, cancelamento e fechamento da competencia;
- criar relatorio e exportacao por loja/competencia.

### Fase 5 - evolucoes opcionais

- aplicativo/area do entregador;
- rastreamento em tempo real;
- notificacoes por WhatsApp/SMS/push;
- agrupamento e otimizacao de varias entregas;
- zonas por poligono, frete gratis por valor minimo e precos por horario.

## Testes de aceite essenciais

1. Retirada cria pedido sem exigir endereco e sem frete.
2. Entrega com endereco valido aplica exatamente uma faixa.
3. Destino fora das faixas nao permite confirmar.
4. Alterar a tabela de frete ou a tarifa nao muda registros anteriores.
5. Total do pedido e do PIX inclui o frete uma unica vez.
6. Entregador de outra loja nao pode ser atribuido.
7. Conclusao gera um unico lancamento de consumo para a loja.
8. Cancelamento antes da conclusao nao gera consumo faturavel.
9. Repetir confirmacao ou callback nao duplica pedido nem lancamento.
10. Falha do provedor de rota nao permite um preco silenciosamente incorreto.

## Decisoes de negocio consolidadas

- o vendedor/loja recebe integralmente pelo frete;
- a loja administra entregadores e responde pela entrega;
- a plataforma atua como balcao online e ferramenta de gestao;
- a plataforma nao intermedeia nem repassa o pagamento do frete;
- a cobranca da plataforma e pelo consumo do recurso de entrega;
- somente entregas concluidas sao computadas, salvo regra contratual diferente.
- cada loja possui percentual individual, inicialmente 2% sobre os fretes das
  entregas concluidas;
- a assinatura tem vigencia de 12 meses e o consumo e incluido em sua cobranca;
- conclusao incorreta e corrigida por evento auditado e credito compensatorio.
- vendedor com permissao gerencial pode corrigir conclusao em ate 7 dias;
- depois da saida, cancelamento depende de decisao e politica da loja;
- openrouteservice Standard gratuito e o provedor inicial do MVP.

## Pontos ainda pendentes

- definir se o consumo acumulado durante a vigencia anual sera faturado
  mensalmente (recomendado) ou somente no fechamento/renovacao anual;
- definir o dia de vencimento das cobrancas;
- definir o texto comercial e juridico da politica de cancelamento da loja;
- criar a conta/chave do openrouteservice e validar no painel os limites vigentes
  antes da producao.

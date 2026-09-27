# Utilização pelo analista

## Antes de receber o executável

O analista deve:

1. instalar o Python 3.12 pela Central de Software;
2. instalar a AWS CLI 2.24 pela Central de Software;
3. solicitar acesso à conta, função e recursos AWS utilizados pela área;
4. ter o Microsoft Excel disponível e permissão para executar a macro assinada ou autorizada pela empresa.

O executável não instala esses programas e não concede acesso à AWS.

## Primeira abertura

1. Feche outras instâncias da planilha.
2. Abra `Ocorrencias.exe`.
3. Se a configuração corporativa estiver habilitada, aguarde a preparação inicial. Ela pode criar arquivos do usuário, variáveis, ambiente virtual e instalar bibliotecas.
4. Se o navegador abrir para autenticação AWS, conclua o login corporativo. Essa etapa fará parte da integração corporativa; não está presente no protótipo que usa CSV.
5. Aguarde a abertura de `consulta.xlsm`.
6. Se o Excel solicitar autorização para macros, siga somente o procedimento homologado pela empresa.

A preparação pode demorar na primeira abertura por causa da instalação das bibliotecas. O marcador `.ambiente-configurado` impede a repetição nas aberturas seguintes.

## Atualização dos dados

1. Abra o executável; evite abrir diretamente uma cópia antiga da planilha.
2. Na aba `Distribuição`, clique em **Atualizar dados**.
3. Aguarde enquanto a barra de status informa `Executando consulta...`.
4. Consulte as abas de resultado.
5. Confira o horário exibido em **Última atualização**.

No sucesso, nenhuma caixa de confirmação é exibida. Em caso de falha, o Excel apresenta uma mensagem para o usuário e não atualiza o horário.

## Aberturas posteriores

O executável verifica sua versão interna, mantém a instalação em `%LOCALAPPDATA%\Ocorrencias` e abre a planilha. A preparação inicial é ignorada quando `%LOCALAPPDATA%\Ocorrencias\.ambiente-configurado` existe.

## Atualização de versão

Quando receber um novo `Ocorrencias.exe`:

1. feche a planilha;
2. substitua o executável anterior pelo novo;
3. abra o novo arquivo normalmente.

O EXE atualiza a cópia instalada ao detectar uma assinatura de pacote diferente. Não é necessário localizar ou editar a pasta em `%LOCALAPPDATA%`.

## Cuidados

- Não altere arquivos dentro de `%LOCALAPPDATA%\Ocorrencias`.
- Não renomeie abas de resultado sem orientação do responsável.
- Não compartilhe mensagens técnicas, logs ou arquivos corporativos fora dos canais autorizados.
- Nunca adicione credenciais permanentes aos arquivos do pacote.

Se ocorrer erro, anote a mensagem exibida, o horário e a ação realizada, e consulte [Solução de problemas](SOLUCAO_DE_PROBLEMAS.md).


# Configuração do ambiente corporativo

> Consulte também a [documentação principal](../README.md), a [arquitetura](../docs/ARQUITETURA.md) e o guia de [manutenção](../docs/MANUTENCAO.md).

Esta estrutura está desativada por padrão e não altera o computador enquanto `Habilitado` estiver como `false` em `ambiente.json`.

Todas as variáveis são criadas exclusivamente no escopo do usuário atual. O projeto nunca cria variáveis globais da máquina.

No ambiente de trabalho:

1. coloque o arquivo AWS em `arquivos/aws/config`;
2. coloque o certificado em `arquivos/certificado/corporate-ca-bundle.pem`;
3. coloque o arquivo do pip em `arquivos/pip/pip.ini`;
4. preencha os valores de `HTTP_PROXY`, `HTTPS_PROXY` e `NO_PROXY`;
5. confirme os caminhos de destino;
6. habilite individualmente as regras;
7. revise a seção `Python` e o arquivo `bibliotecas/requirements.txt`;
8. habilite `Python` quando quiser criar o ambiente virtual e instalar as bibliotecas;
9. por último, altere `Habilitado` no início do JSON para `true`;
10. gere novamente o executável.

`AWS_CA_BUNDLE` já está preparado para apontar para `%APPDATA%\.aws\corporate-ca-bundle.pem`.

O lançador cria somente variáveis e arquivos ausentes. Valores e arquivos existentes são sempre preservados.

O executável localiza automaticamente o Python 3.12 usando primeiro o Python Launcher (`py -3.12`) e depois `where.exe python` como alternativa. Não é necessário informar o caminho do usuário.

Quando `CriarAmbienteVirtual` estiver ativo, será criado `%LOCALAPPDATA%\Ocorrencias\runtime\.venv`. O comando pip usa o Python desse ambiente. A pasta `Scripts` passa a existir durante a criação do ambiente e só depois é adicionada ao `Path` do usuário, sem remover entradas anteriores.

A preparação respeita esta ordem:

1. copia os arquivos habilitados que ainda não existirem;
2. cria as variáveis de usuário ausentes e as aplica ao processo atual;
3. localiza o Python 3.12;
4. cria o ambiente virtual e, com isso, sua pasta `Scripts`;
5. executa `ComandoPip` usando o Python do ambiente virtual;
6. registra os kernels do Glue, quando habilitado;
7. acrescenta os caminhos descobertos ao `Path` do usuário;
8. grava `runtime\python-executavel.txt` para a macro;
9. cria `.ambiente-configurado` somente após todas as etapas concluírem.

`bibliotecas/requirements.txt` registra as dependências. `ComandoPip` pode ser personalizado com quaisquer argumentos aceitos pelo Python. `%APPDIR%` representa a pasta instalada da aplicação.

Se `InstalarKernelsGlue` estiver ativo, `install-glue-kernels` será executado somente depois da instalação de `aws-glue-sessions`.

Se alguma etapa falhar ou exceder `TimeoutMinutos`, `.ambiente-configurado` não será criado. O detalhe ficará em `%LOCALAPPDATA%\Ocorrencias\logs\preparacao-python.log`.

Depois que todas as regras terminarem com sucesso, é criado `%LOCALAPPDATA%\Ocorrencias\.ambiente-configurado`. Nas próximas aberturas, a preparação é ignorada. Para repetir a conferência durante os testes, remova somente esse marcador; mesmo assim, os itens existentes não serão sobrescritos.

Não coloque senhas, tokens, access keys, secret keys ou chaves privadas nesta pasta.

## Observação sobre o caminho AWS

O padrão documentado pela AWS no Windows é `%USERPROFILE%\.aws\config`. O valor atual do projeto usa `%APPDATA%\.aws\config` porque esse foi o local lembrado durante a prototipação. Confirme o caminho corporativo real antes de habilitar a regra e ajuste `Destino` em `ambiente.json` se necessário.

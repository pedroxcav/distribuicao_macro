# Como gerar o executável

> Para o processo completo de manutenção, teste e publicação, consulte o [guia de manutenção](../docs/MANUTENCAO.md).

## Estrutura necessária

```text
excel-python/
├── consulta.xlsm
├── build/
│   ├── Program.cs
│   └── ocorrencias.ico
├── configuracao/
│   ├── ambiente.json
│   ├── LEIA-ME.md
│   └── arquivos/
├── exe/
│   ├── gerar-exe.ps1
│   └── como_gerar_exe.md
├── exemplo/
│   └── dados_exemplo.csv
├── python/
│   └── consulta.py
└── vba/
    └── ModuloConsulta.bas
```

## Geração

Antes de gerar uma versão, confirme que `consulta.xlsm` contém a versão atual de `vba\ModuloConsulta.bas`. A pasta `vba` é a fonte de manutenção; o gerador incorpora a planilha como ela está e não injeta o módulo automaticamente.

Abra o PowerShell na pasta do projeto e execute:

```powershell
.\exe\gerar-exe.ps1
```

O resultado será criado em:

```text
dist\Ocorrencias.exe
```

Se a política de execução do Windows impedir o comando, utilize apenas para esta execução:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\exe\gerar-exe.ps1
```

## Conteúdo incorporado

O gerador inclui no executável:

- `consulta.xlsm`;
- pasta `python`;
- pasta `exemplo`;
- pasta `vba`, quando existir.
- pasta `configuracao`.

Arquivos temporários do Excel, resultados anteriores, `.idea`, `build` e `dist` não são incluídos.

## Atualizações

O executável calcula a assinatura do pacote interno. Quando uma nova compilação possuir conteúdo diferente, ela atualizará automaticamente a cópia instalada em:

```text
%LOCALAPPDATA%\Ocorrencias
```

Feche a planilha antes de executar uma versão atualizada, pois o Excel mantém o arquivo `consulta.xlsm` bloqueado enquanto ele está aberto.

O processo não instala o Python base nem a AWS CLI e não valida permissões na conta AWS. Esses pré-requisitos continuam sendo responsabilidade do usuário. Quando a preparação estiver habilitada, o lançador apenas localiza a instalação do Python 3.12 para criar o ambiente virtual; se ela não estiver acessível, exibe um erro em vez de tentar instalá-la.

## Preparação do ambiente corporativo

A configuração fica desativada por padrão. Edite `configuracao\ambiente.json`, adicione os arquivos reais nas subpastas indicadas e habilite as regras somente depois de confirmar os valores no ambiente de trabalho.

Quando habilitado, o lançador pode:

- criar `HTTP_PROXY`, `HTTPS_PROXY`, `NO_PROXY` e `AWS_CA_BUNDLE` para o usuário;
- localizar automaticamente o Python 3.12;
- criar um ambiente virtual dentro da aplicação;
- executar um comando `pip` configurável;
- registrar opcionalmente os kernels do AWS Glue;
- acrescentar ao `Path` do usuário os diretórios descobertos, sem remover entradas existentes;
- copiar o arquivo AWS para `%APPDATA%\.aws\config`;
- copiar o certificado e fazer `AWS_CA_BUNDLE` apontar para ele;
- copiar `pip.ini` para `%APPDATA%\pip\pip.ini`;
- criar somente variáveis e arquivos ausentes, preservando tudo que já existir.

O lançador aplica os mesmos valores também ao próprio processo, para que o Excel aberto em seguida já os receba. Na primeira configuração, mantenha todas as instâncias do Excel fechadas.

O escopo é fixo no código: nenhuma variável global da máquina é criada.

Não é necessário informar o caminho do `python.exe`. A seção `Python` define somente a versão, o uso do ambiente virtual, o comando pip, o registro dos kernels e a atualização dinâmica do `Path`.

Por padrão, `ComandoPip` executa `-m pip install --upgrade -r` usando o `requirements.txt` incorporado. A etapa só é marcada como concluída se todos os comandos retornarem código zero.

A ordem é intencional: localizar o Python, criar o ambiente virtual, instalar as bibliotecas, registrar os kernels do Glue e somente então acrescentar os diretórios descobertos ao `Path`. Assim, a pasta `Scripts` já existe quando é registrada.

Depois da primeira preparação concluída, o lançador cria `%LOCALAPPDATA%\Ocorrencias\.ambiente-configurado` e ignora essa etapa nas próximas aberturas. O marcador somente é gravado após a conclusão de todas as regras habilitadas.

## Ícone

Para mudar o ícone, substitua `build\ocorrencias.ico` por outro arquivo `.ico` e gere novamente o executável.

## Distribuição corporativa

Antes da distribuição definitiva, assine digitalmente o executável e a macro conforme as políticas da empresa.

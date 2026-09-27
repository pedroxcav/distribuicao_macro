# Solução de problemas

## O EXE não apareceu em `dist`

O gerador sempre usa o nome `dist/Ocorrencias.exe` e sobrescreve a versão anterior. Atualize o Explorador com `F5` e confira data e tamanho:

```powershell
Get-Item .\dist\Ocorrencias.exe
Get-FileHash .\dist\Ocorrencias.exe -Algorithm SHA256
```

Execute a geração a partir da raiz do projeto:

```powershell
.\exe\gerar-exe.ps1
```

## O Excel informa que o Python não foi encontrado

Possíveis causas:

- o Python 3.12 não foi instalado;
- a preparação Python está desabilitada;
- `runtime/python-executavel.txt` não foi criado;
- a planilha ainda contém uma versão antiga da macro com caminho pessoal fixo;
- o ambiente virtual foi removido depois da preparação.

Confirme o arquivo:

```text
%LOCALAPPDATA%\Ocorrencias\runtime\python-executavel.txt
```

Em desenvolvimento, confirme também que `consulta.xlsm` recebeu a versão atual de `vba/ModuloConsulta.bas`.

## A preparação falhou

Consulte:

```text
%LOCALAPPDATA%\Ocorrencias\logs\preparacao-python.log
```

O marcador `.ambiente-configurado` não é criado quando uma etapa falha. Corrija a causa e abra novamente o EXE.

Causas comuns:

- proxy ou certificado incorreto;
- `pip.ini` ausente ou inválido;
- pacote indisponível no repositório corporativo;
- versão Python incorreta;
- `install-glue-kernels` não criado após a instalação;
- timeout baixo para a rede corporativa.

## Preciso repetir a preparação

Feche Excel e EXE. Remova somente:

```text
%LOCALAPPDATA%\Ocorrencias\.ambiente-configurado
```

Na próxima abertura, as regras habilitadas serão avaliadas novamente. Arquivos e variáveis existentes continuarão preservados; removê-lo não força sobrescrita.

Não remova toda a pasta da aplicação sem antes confirmar que não há resultados ou arquivos necessários.

## Uma nova configuração não foi aplicada

Atualizar o EXE muda `.pacote.sha256` e extrai o novo conteúdo, mas não remove `.ambiente-configurado`. Quando uma versão exigir nova preparação, inclua a remoção controlada do marcador no procedimento de atualização ou implemente versionamento específico para a configuração.

## Erro “dados fora da configuração esperada”

A fonte retornou pelo menos um valor de `tipo_atividade` sem rota. Isso é intencional: os registros não são ignorados nem enviados a uma aba genérica.

O responsável deve:

1. confirmar se o analítico buscou somente os tipos previstos;
2. conferir o detalhe técnico na execução do Python;
3. adicionar a rota correta em `ROTAS`, se o novo tipo for legítimo;
4. testar e publicar uma nova versão.

## Erro de coluna ausente ou valores inválidos

Confirme `COLUNA_ROTEAMENTO` em `python/consulta.py` e o nome retornado pela fonte. A coluna aceita valores inteiros; vazios, textos não numéricos e valores fracionários causam falha.

## O Excel não atualizou as abas

Verifique:

- se `saida/manifesto.csv` existe;
- se os CSVs indicados no manifesto existem;
- se a macro está habilitada;
- se os nomes das abas no manifesto são válidos;
- se a planilha não está em modo protegido ou somente leitura.

O Python grava a mensagem destinada ao Excel em:

```text
%LOCALAPPDATA%\Ocorrencias\saida\erro_consulta.txt
```

## A planilha está bloqueada durante a atualização do EXE

Feche todas as janelas que usam `consulta.xlsm` e execute o novo EXE novamente. O Excel mantém o arquivo aberto e impede que o lançador o substitua.

## O login AWS abre repetidamente

Na futura integração AWS, confirme:

```powershell
aws sts get-caller-identity --profile sosdados --no-cli-pager
```

Se falhar, confirme acesso à conta, perfil `sosdados`, proxy, certificado, relógio do computador e validade da sessão SSO. Em seguida:

```powershell
aws sso login --profile sosdados --no-cli-pager
```

O protótipo atual não executa esses comandos automaticamente.

## Antivírus ou Windows bloqueou o EXE

Não desative controles de segurança. A versão corporativa deve ser assinada e distribuída pelo canal aprovado. Encaminhe o hash do arquivo e a mensagem do produto de segurança para a equipe responsável.

## Onde buscar evidências

| Evidência | Caminho |
|---|---|
| Log de preparação Python | `%LOCALAPPDATA%\Ocorrencias\logs\preparacao-python.log` |
| Erro da consulta | `%LOCALAPPDATA%\Ocorrencias\saida\erro_consulta.txt` |
| Manifesto atual | `%LOCALAPPDATA%\Ocorrencias\saida\manifesto.csv` |
| Python usado pela macro | `%LOCALAPPDATA%\Ocorrencias\runtime\python-executavel.txt` |
| Versão do payload | `%LOCALAPPDATA%\Ocorrencias\.pacote.sha256` |
| Preparação concluída | `%LOCALAPPDATA%\Ocorrencias\.ambiente-configurado` |


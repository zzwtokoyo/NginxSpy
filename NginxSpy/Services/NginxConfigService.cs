using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using NginxSpy.Models;
using NginxSpy.Services.Interfaces;

namespace NginxSpy.Services;

/// <summary>
/// Nginx配置文件解析服务实现
/// </summary>
public class NginxConfigService : INginxConfigService
{
    private readonly ILogger<NginxConfigService> _logger;
    private readonly INginxRepository _repository;

    // 常用nginx指令的正则表达式
    private static readonly Regex DirectiveRegex = new(@"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s+([^;{]+)\s*[;{]?\s*$", RegexOptions.Compiled);
    private static readonly Regex BlockStartRegex = new(@"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*([^{]*)\s*{\s*$", RegexOptions.Compiled);
    private static readonly Regex BlockEndRegex = new(@"^\s*}\s*$", RegexOptions.Compiled);
    private static readonly Regex CommentRegex = new(@"^\s*#.*$", RegexOptions.Compiled);
    private static readonly Regex IncludeRegex = new(@"^\s*include\s+([^;]+);\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public NginxConfigService(ILogger<NginxConfigService> logger, INginxRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    /// <summary>
    /// 解析nginx配置文件
    /// </summary>
    public async Task<NginxConfig> ParseConfigAsync(string configPath)
    {
        try
        {
            _logger.LogInformation($"开始解析配置文件: {configPath}");

            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException($"配置文件不存在: {configPath}");
            }

            var content = await File.ReadAllTextAsync(configPath, Encoding.UTF8);
            var config = new NginxConfig
            {
                FilePath = configPath,
                Content = content,
                LastModified = File.GetLastWriteTime(configPath)
            };

            // 解析配置段
            config.Sections = await ParseSectionsAsync(content);

            // 验证配置
            var validationResult = await ValidateConfigAsync(config);
            config.IsValid = validationResult.IsValid;

            _logger.LogInformation($"配置文件解析完成: {configPath}, 有效性: {config.IsValid}");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"解析配置文件失败: {configPath}");
            throw;
        }
    }

    /// <summary>
    /// 保存nginx配置文件
    /// </summary>
    public async Task<bool> SaveConfigAsync(NginxConfig config, string configPath)
    {
        try
        {
            _logger.LogInformation($"保存配置文件: {configPath}");

            // 直接使用配置对象中的内容，不重新生成
            var content = config.Content;

            // 备份原文件
            if (File.Exists(configPath))
            {
                await BackupConfigAsync(configPath);
            }

            // 保存新文件（使用UTF8无BOM编码）
            var utf8NoBom = new UTF8Encoding(false);
            await File.WriteAllTextAsync(configPath, content, utf8NoBom);

            // 更新配置对象
            config.FilePath = configPath;
            config.LastModified = DateTime.Now;

            _logger.LogInformation($"配置文件保存成功: {configPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"保存配置文件失败: {configPath}");
            return false;
        }
    }

    /// <summary>
    /// 验证nginx配置文件语法
    /// </summary>
    public async Task<ConfigValidationResult> ValidateConfigAsync(NginxConfig config)
    {
        var result = new ConfigValidationResult();

        try
        {
            // 基本语法检查
            await ValidateBasicSyntaxAsync(config.Content, result);

            // 指令检查
            await ValidateDirectivesAsync(config.Content, result);

            // 文件引用检查
            await ValidateIncludeFilesAsync(config.FilePath, result);

            result.IsValid = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证配置文件时发生错误");
            result.Errors.Add(new ConfigError
            {
                Message = $"验证过程中发生错误: {ex.Message}",
                ErrorType = ConfigErrorType.SyntaxError
            });
            result.IsValid = false;
        }

        return result;
    }

    /// <summary>
    /// 验证nginx配置文件语法（通过文件路径）
    /// </summary>
    public async Task<ConfigValidationResult> ValidateConfigFileAsync(string configPath)
    {
        try
        {
            // 使用nginx -t命令验证配置文件
            var result = await ValidateWithNginxCommandAsync(configPath);
            
            // 如果nginx命令验证失败，使用内置验证
            if (!result.IsValid)
            {
                var config = await ParseConfigAsync(configPath);
                return await ValidateConfigAsync(config);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"验证配置文件失败: {configPath}");
            return new ConfigValidationResult
            {
                IsValid = false,
                Errors = new List<ConfigError>
                {
                    new ConfigError
                    {
                        Message = $"验证失败: {ex.Message}",
                        ErrorType = ConfigErrorType.SyntaxError
                    }
                }
            };
        }
    }

    /// <summary>
    /// 获取配置文件的指令列表
    /// </summary>
    public async Task<IEnumerable<ConfigDirective>> ParseDirectivesAsync(string content)
    {
        return await Task.Run(() =>
        {
            var directives = new List<ConfigDirective>();
            var lines = content.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || CommentRegex.IsMatch(line))
                    continue;

                var match = DirectiveRegex.Match(line);
                if (match.Success)
                {
                    var directive = new ConfigDirective
                    {
                        Name = match.Groups[1].Value,
                        Parameters = match.Groups[2].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList(),
                        LineNumber = i + 1
                    };

                    directives.Add(directive);
                }
            }

            return directives;
        });
    }

    /// <summary>
    /// 格式化配置文件内容
    /// </summary>
    public async Task<string> FormatConfigContentAsync(string content)
    {
        return await Task.Run(() =>
        {
            var lines = content.Split('\n');
            var formattedLines = new List<string>();
            int indentLevel = 0;
            const string indent = "    "; // 4个空格缩进

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    formattedLines.Add("");
                    continue;
                }

                // 处理块结束
                if (BlockEndRegex.IsMatch(trimmedLine))
                {
                    indentLevel = Math.Max(0, indentLevel - 1);
                }

                // 添加缩进
                var indentedLine = string.Concat(Enumerable.Repeat(indent, indentLevel)) + trimmedLine;
                formattedLines.Add(indentedLine);

                // 处理块开始
                if (BlockStartRegex.IsMatch(trimmedLine))
                {
                    indentLevel++;
                }
            }

            return string.Join(Environment.NewLine, formattedLines);
        });
    }

    /// <summary>
     /// 生成配置文件内容
     /// </summary>
     public async Task<string> GenerateConfigContentAsync(NginxConfig config)
     {
         return await Task.Run(() =>
         {
             var sb = new StringBuilder();
             
             // 添加文件头注释
             sb.AppendLine($"# Nginx configuration file");
             sb.AppendLine($"# Generated by Nginx Spy at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
             sb.AppendLine();

             // 生成配置段
             foreach (var section in config.Sections.Where(s => s.ParentId == null))
             {
                 GenerateSectionContent(sb, section, 0);
             }

             return sb.ToString();
         });
     }

     /// <summary>
     /// 从配置段列表生成配置文件内容
     /// </summary>
     public async Task<string> GenerateConfigContentAsync(List<ConfigSection> sections)
     {
         return await Task.Run(() =>
         {
             var sb = new StringBuilder();
             
             // 添加文件头注释
             sb.AppendLine($"# Nginx configuration file");
             sb.AppendLine($"# Generated by Nginx Spy at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
             sb.AppendLine();

             // 生成配置段
             foreach (var section in sections.Where(s => s.Parent == null))
             {
                 GenerateSectionContent(sb, section, 0);
             }

             return sb.ToString();
         });
     }

    /// <summary>
    /// 备份配置文件
    /// </summary>
    public async Task<string> BackupConfigAsync(string configPath)
    {
        try
        {
            var backupPath = $"{configPath}.backup.{DateTime.Now:yyyyMMddHHmmss}";
            await Task.Run(() => File.Copy(configPath, backupPath));
            
            _logger.LogInformation($"配置文件备份成功: {backupPath}");
            return backupPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"备份配置文件失败: {configPath}");
            throw;
        }
    }

    /// <summary>
    /// 恢复配置文件
    /// </summary>
    public async Task<bool> RestoreConfigAsync(string configPath, string backupPath)
    {
        try
        {
            if (!File.Exists(backupPath))
            {
                _logger.LogWarning($"备份文件不存在: {backupPath}");
                return false;
            }

            await Task.Run(() => File.Copy(backupPath, configPath, true));
            
            _logger.LogInformation($"配置文件恢复成功: {configPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"恢复配置文件失败: {configPath}");
            return false;
        }
    }

    /// <summary>
    /// 获取配置文件模板
    /// </summary>
    public async Task<string> GetConfigTemplateAsync(ConfigTemplateType templateType)
    {
        return await Task.Run(() =>
        {
            return templateType switch
            {
                ConfigTemplateType.Basic => GetBasicTemplate(),
                ConfigTemplateType.WebServer => GetWebServerTemplate(),
                ConfigTemplateType.ReverseProxy => GetReverseProxyTemplate(),
                ConfigTemplateType.LoadBalancer => GetLoadBalancerTemplate(),
                ConfigTemplateType.SSL => GetSSLTemplate(),
                _ => GetBasicTemplate()
            };
        });
    }

    /// <summary>
    /// 检查配置文件是否存在包含文件
    /// </summary>
    public async Task<IEnumerable<string>> GetIncludedFilesAsync(string configPath)
    {
        try
        {
            var content = await File.ReadAllTextAsync(configPath, Encoding.UTF8);
            var includedFiles = new List<string>();
            var lines = content.Split('\n');
            var configDir = Path.GetDirectoryName(configPath) ?? "";

            foreach (var line in lines)
            {
                var match = IncludeRegex.Match(line.Trim());
                if (match.Success)
                {
                    var includePath = match.Groups[1].Value.Trim().Trim('"', '\'');
                    
                    // 处理相对路径
                    if (!Path.IsPathRooted(includePath))
                    {
                        includePath = Path.Combine(configDir, includePath);
                    }

                    // 处理通配符
                    if (includePath.Contains('*'))
                    {
                        var directory = Path.GetDirectoryName(includePath) ?? "";
                        var pattern = Path.GetFileName(includePath);
                        
                        if (Directory.Exists(directory))
                        {
                            var matchingFiles = Directory.GetFiles(directory, pattern);
                            includedFiles.AddRange(matchingFiles);
                        }
                    }
                    else if (File.Exists(includePath))
                    {
                        includedFiles.Add(includePath);
                    }
                }
            }

            return includedFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"获取包含文件失败: {configPath}");
            return Enumerable.Empty<string>();
        }
    }

    /// <summary>
    /// 合并配置文件（包括include文件）
    /// </summary>
    public async Task<string> MergeConfigFilesAsync(string mainConfigPath)
    {
        try
        {
            var mergedContent = new StringBuilder();
            await MergeConfigRecursiveAsync(mainConfigPath, mergedContent, new HashSet<string>());
            return mergedContent.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"合并配置文件失败: {mainConfigPath}");
            throw;
        }
    }

    #region 私有方法

    /// <summary>
    /// 解析配置段
    /// </summary>
    private async Task<List<ConfigSection>> ParseSectionsAsync(string content)
    {
        return await Task.Run(() =>
        {
            var sections = new List<ConfigSection>();
            var lines = content.Split('\n');
            var sectionStack = new Stack<ConfigSection>();
            var sectionIdCounter = 1;
            var currentSection = new ConfigSection 
            { 
                Id = sectionIdCounter++,
                SectionType = ConfigSectionTypes.Main,
                StartLineNumber = 1,
                EndLineNumber = lines.Length
            };
            sections.Add(currentSection);

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                var lineNumber = i + 1; // 行号从1开始
                
                if (string.IsNullOrEmpty(line) || CommentRegex.IsMatch(line))
                    continue;

                var blockStartMatch = BlockStartRegex.Match(line);
                if (blockStartMatch.Success)
                {
                    var newSection = new ConfigSection
                    {
                        Id = sectionIdCounter++,
                        SectionType = blockStartMatch.Groups[1].Value.ToLower(),
                        Name = blockStartMatch.Groups[2].Value.Trim(),
                        ParentId = currentSection?.Id,
                        StartLineNumber = lineNumber
                    };

                    sections.Add(newSection);
                    sectionStack.Push(currentSection!);
                    currentSection = newSection;
                }
                else if (BlockEndRegex.IsMatch(line))
                {
                    if (currentSection != null)
                    {
                        currentSection.EndLineNumber = lineNumber;
                    }
                    
                    if (sectionStack.Count > 0)
                    {
                        currentSection = sectionStack.Pop();
                    }
                }
                else
                {
                    // 添加指令到当前段
                    if (currentSection != null)
                    {
                        currentSection.Content += line + Environment.NewLine;
                    }
                }
            }

            return sections;
        });
    }

    /// <summary>
    /// 验证基本语法
    /// </summary>
    private async Task ValidateBasicSyntaxAsync(string content, ConfigValidationResult result)
    {
        await Task.Run(() =>
        {
            var lines = content.Split('\n');
            var braceCount = 0;

            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || CommentRegex.IsMatch(line))
                    continue;

                // 检查大括号匹配
                braceCount += line.Count(c => c == '{');
                braceCount -= line.Count(c => c == '}');

                if (braceCount < 0)
                {
                    result.Errors.Add(new ConfigError
                    {
                        Message = "多余的右大括号",
                        LineNumber = i + 1,
                        ErrorType = ConfigErrorType.SyntaxError
                    });
                }

                // 检查分号
                if (!line.EndsWith('{') && !line.EndsWith('}') && !line.EndsWith(';') && !CommentRegex.IsMatch(line))
                {
                    result.Warnings.Add(new ConfigWarning
                    {
                        Message = "指令可能缺少分号",
                        LineNumber = i + 1,
                        WarningType = ConfigWarningType.ConfigurationSuggestion
                    });
                }
            }

            if (braceCount != 0)
            {
                result.Errors.Add(new ConfigError
                {
                    Message = "大括号不匹配",
                    ErrorType = ConfigErrorType.SyntaxError
                });
            }
        });
    }

    /// <summary>
    /// 验证指令
    /// </summary>
    private async Task ValidateDirectivesAsync(string content, ConfigValidationResult result)
    {
        await Task.Run(() =>
        {
            var directives = ParseDirectivesAsync(content).Result;
            var validDirectives = GetValidDirectives();

            foreach (var directive in directives)
            {
                if (!validDirectives.Contains(directive.Name.ToLower()))
                {
                    result.Warnings.Add(new ConfigWarning
                    {
                        Message = $"未知指令: {directive.Name}",
                        LineNumber = directive.LineNumber,
                        WarningType = ConfigWarningType.ConfigurationSuggestion
                    });
                }
            }
        });
    }

    /// <summary>
    /// 验证包含文件
    /// </summary>
    private async Task ValidateIncludeFilesAsync(string configPath, ConfigValidationResult result)
    {
        try
        {
            var includedFiles = await GetIncludedFilesAsync(configPath);
            
            foreach (var file in includedFiles)
            {
                if (!File.Exists(file))
                {
                    result.Errors.Add(new ConfigError
                    {
                        Message = $"包含的文件不存在: {file}",
                        ErrorType = ConfigErrorType.FileNotFound
                    });
                }
            }
        }
        catch (Exception ex)
        {
            result.Warnings.Add(new ConfigWarning
            {
                Message = $"验证包含文件时发生错误: {ex.Message}",
                WarningType = ConfigWarningType.ConfigurationSuggestion
            });
        }
    }

    /// <summary>
    /// 使用nginx命令验证配置
    /// </summary>
    private async Task<ConfigValidationResult> ValidateWithNginxCommandAsync(string configPath)
    {
        var result = new ConfigValidationResult();

        try
        {
            // 尝试找到nginx可执行文件
            var nginxPath = await FindNginxExecutableAsync();
            if (string.IsNullOrEmpty(nginxPath))
            {
                result.Warnings.Add(new ConfigWarning
                {
                    Message = "未找到nginx可执行文件，使用内置验证",
                    WarningType = ConfigWarningType.ConfigurationSuggestion
                });
                return result;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = nginxPath,
                Arguments = $"-t -c \"{configPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();

                result.IsValid = process.ExitCode == 0;
                
                if (!result.IsValid && !string.IsNullOrEmpty(error))
                {
                    result.Errors.Add(new ConfigError
                    {
                        Message = error,
                        ErrorType = ConfigErrorType.SyntaxError
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "使用nginx命令验证配置失败");
            result.Warnings.Add(new ConfigWarning
            {
                Message = $"nginx命令验证失败: {ex.Message}",
                WarningType = ConfigWarningType.ConfigurationSuggestion
            });
        }

        return result;
    }

    /// <summary>
    /// 查找nginx可执行文件
    /// </summary>
    private async Task<string> FindNginxExecutableAsync()
    {
        return await Task.Run(() =>
        {
            var possiblePaths = new[]
            {
                "nginx",
                "nginx.exe",
                @"C:\nginx\nginx.exe",
                @"C:\Program Files\nginx\nginx.exe",
                @"C:\Program Files (x86)\nginx\nginx.exe"
            };

            foreach (var path in possiblePaths)
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = "-v",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using var process = Process.Start(startInfo);
                    if (process != null)
                    {
                        process.WaitForExit(5000);
                        if (process.ExitCode == 0)
                        {
                            return path;
                        }
                    }
                }
                catch
                {
                    // 忽略错误，继续尝试下一个路径
                }
            }

            return string.Empty;
        });
    }

    /// <summary>
    /// 递归合并配置文件
    /// </summary>
    private async Task MergeConfigRecursiveAsync(string configPath, StringBuilder mergedContent, HashSet<string> processedFiles)
    {
        if (processedFiles.Contains(configPath))
        {
            _logger.LogWarning($"检测到循环引用: {configPath}");
            return;
        }

        processedFiles.Add(configPath);
        
        var content = await File.ReadAllTextAsync(configPath, Encoding.UTF8);
        var lines = content.Split('\n');

        foreach (var line in lines)
        {
            var match = IncludeRegex.Match(line.Trim());
            if (match.Success)
            {
                var includedFiles = await GetIncludedFilesAsync(configPath);
                foreach (var includedFile in includedFiles)
                {
                    mergedContent.AppendLine($"# Included from: {includedFile}");
                    await MergeConfigRecursiveAsync(includedFile, mergedContent, processedFiles);
                }
            }
            else
            {
                mergedContent.AppendLine(line);
            }
        }
    }

    /// <summary>
    /// 生成配置段内容
    /// </summary>
    private void GenerateSectionContent(StringBuilder sb, ConfigSection section, int indentLevel)
    {
        var indent = new string(' ', indentLevel * 4);

        if (section.SectionType != ConfigSectionTypes.Main)
        {
            sb.AppendLine($"{indent}{section.SectionType} {section.Name} {{");
            indentLevel++;
            indent = new string(' ', indentLevel * 4);
        }

        // 添加段内容
        if (!string.IsNullOrEmpty(section.Content))
        {
            var lines = section.Content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                sb.AppendLine($"{indent}{line.Trim()}");
            }
        }

        // 添加子段
        foreach (var child in section.Children)
        {
            GenerateSectionContent(sb, child, indentLevel);
        }

        if (section.SectionType != ConfigSectionTypes.Main)
        {
            sb.AppendLine($"{new string(' ', (indentLevel - 1) * 4)}}}");
        }
    }

    /// <summary>
    /// 获取有效指令列表
    /// </summary>
    private HashSet<string> GetValidDirectives()
    {
        return new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "worker_processes", "worker_connections", "listen", "server_name", "root", "index",
            "location", "proxy_pass", "proxy_set_header", "upstream", "server", "include",
            "error_log", "access_log", "gzip", "ssl_certificate", "ssl_certificate_key",
            "return", "rewrite", "try_files", "client_max_body_size", "keepalive_timeout"
        };
    }

    /// <summary>
    /// 获取基本模板
    /// </summary>
    private string GetBasicTemplate()
    {
        return @"worker_processes auto;
error_log logs/error.log;

events {
    worker_connections 1024;
}

http {
    include mime.types;
    default_type application/octet-stream;
    
    sendfile on;
    keepalive_timeout 65;
    
    server {
        listen 80;
        server_name localhost;
        
        location / {
            root html;
            index index.html index.htm;
        }
        
        error_page 500 502 503 504 /50x.html;
        location = /50x.html {
            root html;
        }
    }
}";
    }

    /// <summary>
    /// 获取Web服务器模板
    /// </summary>
    private string GetWebServerTemplate()
    {
        return GetBasicTemplate(); // 简化实现
    }

    /// <summary>
    /// 获取反向代理模板
    /// </summary>
    private string GetReverseProxyTemplate()
    {
        return GetBasicTemplate(); // 简化实现
    }

    /// <summary>
    /// 获取负载均衡模板
    /// </summary>
    private string GetLoadBalancerTemplate()
    {
        return GetBasicTemplate(); // 简化实现
    }

    /// <summary>
    /// 获取SSL模板
    /// </summary>
    private string GetSSLTemplate()
    {
        return GetBasicTemplate(); // 简化实现
    }

    #endregion
}
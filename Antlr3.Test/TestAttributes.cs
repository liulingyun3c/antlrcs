/*
 * [The "BSD licence"]
 * Copyright (c) 2005-2008 Terence Parr
 * All rights reserved.
 *
 * Conversion to C#:
 * Copyright (c) 2008 Sam Harwell, Pixel Mine, Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 * 1. Redistributions of source code must retain the above copyright
 *    notice, this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright
 *    notice, this list of conditions and the following disclaimer in the
 *    documentation and/or other materials provided with the distribution.
 * 3. The name of the author may not be used to endorse or promote products
 *    derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace AntlrUnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Antlr.Runtime.JavaExtensions;
    using Antlr3.Grammars;
    using Antlr3.Tool;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using ActionTranslator = Antlr3.Grammars.ActionTranslator;
    using AngleBracketTemplateLexer = Antlr3.ST.Language.AngleBracketTemplateLexer;
    using AntlrTool = Antlr3.AntlrTool;
    using CodeGenerator = Antlr3.Codegen.CodeGenerator;
    using CommonToken = Antlr.Runtime.CommonToken;
    using StringReader = System.IO.StringReader;
    using StringTemplate = Antlr3.ST.StringTemplate;
    using StringTemplateGroup = Antlr3.ST.StringTemplateGroup;

    /** Check the $x, $x.y attributes.  For checking the actual
     *  translation, assume the Java target.  This is still a great test
     *  for the semantics of the $x.y stuff regardless of the target.
     */
    [TestClass]
    public class TestAttributes : BaseTest
    {

        /** Public default constructor used by TestRig */
        public TestAttributes()
        {
        }

        [TestMethod]
        public void TestEscapedLessThanInAction() /*throws Exception*/ {
            Grammar g = new Grammar();
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            string action = "i<3; '<xmltag>'";
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 0 );
            string expecting = action;
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, "<action>" );
            actionST.setAttribute( "action", rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestEscaped_InAction() /*throws Exception*/ {
            string action = "int \\$n; \"\\$in string\\$\"";
            string expecting = "int $n; \"$in string$\"";
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "@members {" + action + "}\n" +
                "a[User u, int i]\n" +
                "        : {" + action + "}\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 0 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestArguments() /*throws Exception*/ {
            string action = "$i; $i.x; $u; $u.x";
            string expecting = "i; i.x; u; u.x";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[User u, int i]\n" +
                "        : {" + action + "}\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestComplicatedArgParsing() /*throws Exception*/ {
            string action = "x, (*a).foo(21,33), 3.2+1, '\\n', " +
                            "\"a,oo\\nick\", {bl, \"fdkj\"eck}";
            string expecting = "x, (*a).foo(21,33), 3.2+1, '\\n', \"a,oo\\nick\", {bl, \"fdkj\"eck}";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );

            // now check in actual grammar.
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[User u, int i]\n" +
                "        : A a[" + action + "] B\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation = translator.translate();
            assertEquals( expecting, rawTranslation );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestBracketArgParsing() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );

            // now check in actual grammar.
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[String[\\] ick, int i]\n" +
                "        : A \n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            Rule r = g.getRule( "a" );
            AttributeScope parameters = r.parameterScope;
            var attrs = parameters.Attributes;
            assertEquals( "attribute mismatch", "String[] ick", attrs.ElementAt( 0 ).Decl.ToString() );
            assertEquals( "parameter name mismatch", "ick", attrs.ElementAt( 0 ).Name );
            assertEquals( "declarator mismatch", "String[]", attrs.ElementAt( 0 ).Type );

            assertEquals( "attribute mismatch", "int i", attrs.ElementAt( 1 ).Decl.ToString() );
            assertEquals( "parameter name mismatch", "i", attrs.ElementAt( 1 ).Name );
            assertEquals( "declarator mismatch", "int", attrs.ElementAt( 1 ).Type );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestStringArgParsing() /*throws Exception*/ {
            string action = "34, '{', \"it's<\", '\"', \"\\\"\", 19";
            string expecting = "34, '{', \"it's<\", '\"', \"\\\"\", 19";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );

            // now check in actual grammar.
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[User u, int i]\n" +
                "        : A a[" + action + "] B\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation = translator.translate();
            assertEquals( expecting, rawTranslation );

            //IList<String> expectArgs = new List<String>() {
            //    {add("34");}
            //    {add("'{'");}
            //    {add("\"it's<\"");}
            //    {add("'\"'");}
            //    {add("\"\\\"\"");} // that's "\""
            //    {add("19");}
            //};
            IList<string> expectArgs = new List<string>( new string[]
            {
                "34",
                "'{'",
                "\"it's<\"",
                "'\"'",
                "\"\\\"\"", // that's "\""
                "19"
            } );

            List<string> actualArgs = CodeGenerator.getListOfArgumentsFromAction( action, ',' );
            //assertEquals( "args mismatch", expectArgs, actualArgs );
            assertTrue( "args mismatch", expectArgs.SequenceEqual( actualArgs ) );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestComplicatedSingleArgParsing() /*throws Exception*/ {
            string action = "(*a).foo(21,33,\",\")";
            string expecting = "(*a).foo(21,33,\",\")";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );

            // now check in actual grammar.
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[User u, int i]\n" +
                "        : A a[" + action + "] B\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation = translator.translate();
            assertEquals( expecting, rawTranslation );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestArgWithLT() /*throws Exception*/ {
            string action = "34<50";
            string expecting = "34<50";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );

            // now check in actual grammar.
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[boolean b]\n" +
                "        : A a[" + action + "] B\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            assertEquals( expecting, rawTranslation );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestGenericsAsArgumentDefinition() /*throws Exception*/ {
            string action = "$foo.get(\"ick\");";
            string expecting = "foo.get(\"ick\");";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            string grammar =
                "parser grammar T;\n" +
                "a[HashMap<String,String> foo]\n" +
                "        : {" + action + "}\n" +
                "        ;";
            Grammar g = new Grammar( grammar );
            Rule ra = g.getRule( "a" );
            var attrs = ra.parameterScope.Attributes;
            assertEquals( "attribute mismatch", "HashMap<String,String> foo", attrs.ElementAt( 0 ).Decl.ToString() );
            assertEquals( "parameter name mismatch", "foo", attrs.ElementAt( 0 ).Name );
            assertEquals( "declarator mismatch", "HashMap<String,String>", attrs.ElementAt( 0 ).Type );

            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestGenericsAsArgumentDefinition2() /*throws Exception*/ {
            string action = "$foo.get(\"ick\"); x=3;";
            string expecting = "foo.get(\"ick\"); x=3;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            string grammar =
                "parser grammar T;\n" +
                "a[HashMap<String,String> foo, int x, List<String> duh]\n" +
                "        : {" + action + "}\n" +
                "        ;";
            Grammar g = new Grammar( grammar );
            Rule ra = g.getRule( "a" );
            var attrs = ra.parameterScope.Attributes;

            assertEquals( "attribute mismatch", "HashMap<String,String> foo", attrs.ElementAt( 0 ).Decl.ToString().Trim() );
            assertEquals( "parameter name mismatch", "foo", attrs.ElementAt( 0 ).Name );
            assertEquals( "declarator mismatch", "HashMap<String,String>", attrs.ElementAt( 0 ).Type );

            assertEquals( "attribute mismatch", "int x", attrs.ElementAt( 1 ).Decl.ToString().Trim() );
            assertEquals( "parameter name mismatch", "x", attrs.ElementAt( 1 ).Name );
            assertEquals( "declarator mismatch", "int", attrs.ElementAt( 1 ).Type );

            assertEquals( "attribute mismatch", "List<String> duh", attrs.ElementAt( 2 ).Decl.ToString().Trim() );
            assertEquals( "parameter name mismatch", "duh", attrs.ElementAt( 2 ).Name );
            assertEquals( "declarator mismatch", "List<String>", attrs.ElementAt( 2 ).Type );

            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestGenericsAsReturnValue() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            string grammar =
                "parser grammar T;\n" +
                "a returns [HashMap<String,String> foo] : ;\n";
            Grammar g = new Grammar( grammar );
            Rule ra = g.getRule( "a" );
            var attrs = ra.returnScope.Attributes;
            assertEquals( "attribute mismatch", "HashMap<String,String> foo", attrs.ElementAt( 0 ).Decl.ToString() );
            assertEquals( "parameter name mismatch", "foo", attrs.ElementAt( 0 ).Name );
            assertEquals( "declarator mismatch", "HashMap<String,String>", attrs.ElementAt( 0 ).Type );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestComplicatedArgParsingWithTranslation() /*throws Exception*/ {
            string action = "x, $A.text+\"3242\", (*$A).foo(21,33), 3.2+1, '\\n', " +
                            "\"a,oo\\nick\", {bl, \"fdkj\"eck}";
            string expecting = "x, (A1!=null?A1.getText():null)+\"3242\", (*A1).foo(21,33), 3.2+1, '\\n', \"a,oo\\nick\", {bl, \"fdkj\"eck}";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );

            // now check in actual grammar.
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[User u, int i]\n" +
                "        : A a[" + action + "] B\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        /** $x.start refs are checked during translation not before so ANTLR misses
         the fact that rule r has refs to predefined attributes if the ref is after
         the def of the method or self-referential.  Actually would be ok if I didn't
         convert actions to strings; keep as templates.
         June 9, 2006: made action translation leave templates not strings
         */
        [TestMethod]
        public void TestRefToReturnValueBeforeRefToPredefinedAttr() /*throws Exception*/ {
            string action = "$x.foo";
            string expecting = "(x!=null?x.foo:0)";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a : x=b {" + action + "} ;\n" +
                "b returns [int foo] : B {$b.start} ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRuleLabelBeforeRefToPredefinedAttr() /*throws Exception*/ {
            // As of Mar 2007, I'm removing unused labels.  Unfortunately,
            // the action is not seen until code gen.  Can't see $x.text
            // before stripping unused labels.  We really need to translate
            // actions first so code gen logic can use info.
            string action = "$x.text";
            string expecting = "(x!=null?input.toString(x.start,x.stop):null)";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a : x=b {" + action + "} ;\n" +
                "b : B ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestInvalidArguments() /*throws Exception*/ {
            string action = "$x";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[User u, int i]\n" +
                "        : {" + action + "}\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_UNKNOWN_SIMPLE_ATTRIBUTE;
            object expectedArg = "x";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestReturnValue() /*throws Exception*/ {
            string action = "$x.i";
            string expecting = "x";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a returns [int i]\n" +
                "        : 'a'\n" +
                "        ;\n" +
                "b : x=a {" + action + "} ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "b",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestReturnValueWithNumber() /*throws Exception*/ {
            string action = "$x.i1";
            string expecting = "x";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a returns [int i1]\n" +
                "        : 'a'\n" +
                "        ;\n" +
                "b : x=a {" + action + "} ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "b",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestReturnValues() /*throws Exception*/ {
            string action = "$i; $i.x; $u; $u.x";
            string expecting = "retval.i; retval.i.x; retval.u; retval.u.x";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a returns [User u, int i]\n" +
                "        : {" + action + "}\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        /* regression test for ANTLR-46 */
        [TestMethod]
        public void TestReturnWithMultipleRuleRefs() /*throws Exception*/ {
            string action1 = "$obj = $rule2.obj;";
            string action2 = "$obj = $rule3.obj;";
            string expecting1 = "obj = rule21;";
            string expecting2 = "obj = rule32;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "rule1 returns [ Object obj ]\n" +
                ":	rule2 { " + action1 + " }\n" +
                "|	rule3 { " + action2 + " }\n" +
                ";\n" +
                "rule2 returns [ Object obj ]\n" +
                ":	foo='foo' { $obj = $foo.text; }\n" +
                ";\n" +
                "rule3 returns [ Object obj ]\n" +
                ":	bar='bar' { $obj = $bar.text; }\n" +
                ";" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            int i = 0;
            string action = action1;
            string expecting = expecting1;
            do
            {
                ActionTranslator translator = new ActionTranslator( generator, "rule1",
                                                                             new CommonToken( ANTLRParser.ACTION, action ), i + 1 );
                string rawTranslation =
                        translator.translate();
                StringTemplateGroup templates =
                        new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
                StringTemplate actionST = new StringTemplate( templates, rawTranslation );
                string found = actionST.ToString();
                assertEquals( expecting, found );
                action = action2;
                expecting = expecting2;
            } while ( i++ < 1 );
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestInvalidReturnValues() /*throws Exception*/ {
            string action = "$x";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a returns [User u, int i]\n" +
                "        : {" + action + "}\n" +
                "        ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_UNKNOWN_SIMPLE_ATTRIBUTE;
            object expectedArg = "x";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestTokenLabels() /*throws Exception*/ {
            string action = "$id; $f; $id.text; $id.getText(); $id.dork " +
                            "$id.type; $id.line; $id.pos; " +
                            "$id.channel; $id.index;";
            string expecting = "id; f; (id!=null?id.getText():null); id.getText(); id.dork (id!=null?id.getType():0); (id!=null?id.getLine():0); (id!=null?id.getCharPositionInLine():0); (id!=null?id.getChannel():0); (id!=null?id.getTokenIndex():0);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a : id=ID f=FLOAT {" + action + "}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRuleLabels() /*throws Exception*/ {
            string action = "$r.x; $r.start;\n $r.stop;\n $r.tree; $a.x; $a.stop;";
            string expecting = "(r!=null?r.x:0); (r!=null?((Token)r.start):null);" + NewLine +
                               "             (r!=null?((Token)r.stop):null);" + NewLine +
                               "             (r!=null?((Object)r.tree):null); (r!=null?r.x:0); (r!=null?((Token)r.stop):null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a returns [int x]\n" +
                "  :\n" +
                "  ;\n" +
                "b : r=a {###" + action + "!!!}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // codegen phase sets some vars we need
            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestAmbiguRuleRef() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a : A a {$a.text} | B ;" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            // error(132): <string>:2:9: reference $a is ambiguous; rule a is enclosing rule and referenced in the production
            assertEquals( "unexpected errors: " + equeue, 1, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRuleLabelsWithSpecialToken() /*throws Exception*/ {
            string action = "$r.x; $r.start; $r.stop; $r.tree; $a.x; $a.stop;";
            string expecting = "(r!=null?r.x:0); (r!=null?((MYTOKEN)r.start):null); (r!=null?((MYTOKEN)r.stop):null); (r!=null?((Object)r.tree):null); (r!=null?r.x:0); (r!=null?((MYTOKEN)r.stop):null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "options {TokenLabelType=MYTOKEN;}\n" +
                "a returns [int x]\n" +
                "  :\n" +
                "  ;\n" +
                "b : r=a {###" + action + "!!!}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // codegen phase sets some vars we need

            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestForwardRefRuleLabels() /*throws Exception*/ {
            string action = "$r.x; $r.start; $r.stop; $r.tree; $a.x; $a.tree;";
            string expecting = "(r!=null?r.x:0); (r!=null?((Token)r.start):null); (r!=null?((Token)r.stop):null); (r!=null?((Object)r.tree):null); (r!=null?r.x:0); (r!=null?((Object)r.tree):null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "b : r=a {###" + action + "!!!}\n" +
                "  ;\n" +
                "a returns [int x]\n" +
                "  : ;\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // codegen phase sets some vars we need

            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestInvalidRuleLabelAccessesParameter() /*throws Exception*/ {
            string action = "$r.z";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[int z] returns [int x]\n" +
                "  :\n" +
                "  ;\n" +
                "b : r=a[3] {" + action + "}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_INVALID_RULE_PARAMETER_REF;
            object expectedArg = "a";
            object expectedArg2 = "z";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestInvalidRuleLabelAccessesScopeAttribute() /*throws Exception*/ {
            string action = "$r.n";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a\n" +
                "scope { int n; }\n" +
                "  :\n" +
                "  ;\n" +
                "b : r=a[3] {" + action + "}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_INVALID_RULE_SCOPE_ATTRIBUTE_REF;
            object expectedArg = "a";
            object expectedArg2 = "n";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestInvalidRuleAttribute() /*throws Exception*/ {
            string action = "$r.blort";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[int z] returns [int x]\n" +
                "  :\n" +
                "  ;\n" +
                "b : r=a[3] {" + action + "}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_UNKNOWN_RULE_ATTRIBUTE;
            object expectedArg = "a";
            object expectedArg2 = "blort";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestMissingRuleAttribute() /*throws Exception*/ {
            string action = "$r";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a[int z] returns [int x]\n" +
                "  :\n" +
                "  ;\n" +
                "b : r=a[3] {" + action + "}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();

            int expectedMsgID = ErrorManager.MSG_ISOLATED_RULE_SCOPE;
            object expectedArg = "r";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestMissingUnlabeledRuleAttribute() /*throws Exception*/ {
            string action = "$a";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a returns [int x]:\n" +
                "  ;\n" +
                "b : a {" + action + "}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();

            int expectedMsgID = ErrorManager.MSG_ISOLATED_RULE_SCOPE;
            object expectedArg = "a";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestNonDynamicAttributeOutsideRule() /*throws Exception*/ {
            string action = "[TestMethod] public void foo() { $x; }";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "@members {'+action+'}\n" +
                "a : ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         null,
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 0 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_ATTRIBUTE_REF_NOT_IN_RULE;
            object expectedArg = "x";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestNonDynamicAttributeOutsideRule2() /*throws Exception*/ {
            string action = "[TestMethod] public void foo() { $x.y; }";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "@members {'+action+'}\n" +
                "a : ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         null,
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 0 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_ATTRIBUTE_REF_NOT_IN_RULE;
            object expectedArg = "x";
            object expectedArg2 = "y";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        // D Y N A M I C A L L Y  S C O P E D  A T T R I B U T E S

        [TestMethod]
        public void TestBasicGlobalScope() /*throws Exception*/ {
            string action = "$Symbols::names.add($id.text);";
            string expecting = "((Symbols_scope)Symbols_stack.peek()).names.add((id!=null?id.getText():null));";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope Symbols {\n" +
                "  int n;\n" +
                "  List names;\n" +
                "}\n" +
                "a scope Symbols; : (id=ID ';' {" + action + "} )+\n" +
                "  ;\n" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestUnknownGlobalScope() /*throws Exception*/ {
            string action = "$Symbols::names.add($id.text);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a scope Symbols; : (id=ID ';' {" + action + "} )+\n" +
                "  ;\n" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );

            assertEquals( "unexpected errors: " + equeue, 2, equeue.errors.Count );

            int expectedMsgID = ErrorManager.MSG_UNKNOWN_DYNAMIC_SCOPE;
            object expectedArg = "Symbols";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestIndexedGlobalScope() /*throws Exception*/ {
            string action = "$Symbols[-1]::names.add($id.text);";
            string expecting =
                "((Symbols_scope)Symbols_stack.elementAt(Symbols_stack.size()-1-1)).names.add((id!=null?id.getText():null));";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope Symbols {\n" +
                "  int n;\n" +
                "  List names;\n" +
                "}\n" +
                "a scope Symbols; : (id=ID ';' {" + action + "} )+\n" +
                "  ;\n" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void Test0IndexedGlobalScope() /*throws Exception*/ {
            string action = "$Symbols[0]::names.add($id.text);";
            string expecting =
                "((Symbols_scope)Symbols_stack.elementAt(0)).names.add((id!=null?id.getText():null));";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope Symbols {\n" +
                "  int n;\n" +
                "  List names;\n" +
                "}\n" +
                "a scope Symbols; : (id=ID ';' {" + action + "} )+\n" +
                "  ;\n" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            assertEquals( expecting, rawTranslation );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestAbsoluteIndexedGlobalScope() /*throws Exception*/ {
            string action = "$Symbols[3]::names.add($id.text);";
            string expecting =
                "((Symbols_scope)Symbols_stack.elementAt(3)).names.add((id!=null?id.getText():null));";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope Symbols {\n" +
                "  int n;\n" +
                "  List names;\n" +
                "}\n" +
                "a scope Symbols; : (id=ID ';' {" + action + "} )+\n" +
                "  ;\n" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            assertEquals( expecting, rawTranslation );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestScopeAndAttributeWithUnderscore() /*throws Exception*/ {
            string action = "$foo_bar::a_b;";
            string expecting = "((foo_bar_scope)foo_bar_stack.peek()).a_b;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope foo_bar {\n" +
                "  int a_b;\n" +
                "}\n" +
                "a scope foo_bar; : (ID {" + action + "} )+\n" +
                "  ;\n" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();

            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestSharedGlobalScope() /*throws Exception*/ {
            string action = "$Symbols::x;";
            string expecting = "((Symbols_scope)Symbols_stack.peek()).x;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope Symbols {\n" +
                "  String x;\n" +
                "}\n" +
                "a\n" +
                "scope { int y; }\n" +
                "scope Symbols;\n" +
                " : b {" + action + "}\n" +
                " ;\n" +
                "b : ID {$Symbols::x=$ID.text} ;\n" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestGlobalScopeOutsideRule() /*throws Exception*/ {
            string action = "public void foo() {$Symbols::names.add('foo');}";
            string expecting = "public void foo() {((Symbols_scope)Symbols_stack.peek()).names.add('foo');}";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope Symbols {\n" +
                "  int n;\n" +
                "  List names;\n" +
                "}\n" +
                "@members {'+action+'}\n" +
                "a : \n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRuleScopeOutsideRule() /*throws Exception*/ {
            string action = "public void foo() {$a::name;}";
            string expecting = "public void foo() {((a_scope)a_stack.peek()).name;}";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "@members {" + action + "}\n" +
                "a\n" +
                "scope { String name; }\n" +
                "  : {foo();}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         null,
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 0 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestBasicRuleScope() /*throws Exception*/ {
            string action = "$a::n;";
            string expecting = "((a_scope)a_stack.peek()).n;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a\n" +
                "scope {\n" +
                "  int n;\n" +
                "} : {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestUnqualifiedRuleScopeAccessInsideRule() /*throws Exception*/ {
            string action = "$n;";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a\n" +
                "scope {\n" +
                "  int n;\n" +
                "} : {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates

            int expectedMsgID = ErrorManager.MSG_ISOLATED_RULE_ATTRIBUTE;
            object expectedArg = "n";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg,
                                            expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestIsolatedDynamicRuleScopeRef() /*throws Exception*/ {
            string action = "$a;"; // refers to stack not top of stack
            string expecting = "a_stack;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a\n" +
                "scope {\n" +
                "  int n;\n" +
                "} : b ;\n" +
                "b : {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestDynamicRuleScopeRefInSubrule() /*throws Exception*/ {
            string action = "$a::n;";
            string expecting = "((a_scope)a_stack.peek()).n;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a\n" +
                "scope {\n" +
                "  float n;\n" +
                "} : b ;\n" +
                "b : {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestIsolatedGlobalScopeRef() /*throws Exception*/ {
            string action = "$Symbols;";
            string expecting = "Symbols_stack;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope Symbols {\n" +
                "  String x;\n" +
                "}\n" +
                "a\n" +
                "scope { int y; }\n" +
                "scope Symbols;\n" +
                " : b {" + action + "}\n" +
                " ;\n" +
                "b : ID {$Symbols::x=$ID.text} ;\n" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRuleScopeFromAnotherRule() /*throws Exception*/ {
            string action = "$a::n;"; // must be qualified
            string expecting = "((a_scope)a_stack.peek()).n;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a\n" +
                "scope {\n" +
                "  boolean n;\n" +
                "} : b\n" +
                "  ;\n" +
                "b : {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestFullyQualifiedRefToCurrentRuleParameter() /*throws Exception*/ {
            string action = "$a.i;";
            string expecting = "i;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a[int i]: {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestFullyQualifiedRefToCurrentRuleRetVal() /*throws Exception*/ {
            string action = "$a.i;";
            string expecting = "retval.i;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a returns [int i, int j]: {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestSetFullyQualifiedRefToCurrentRuleRetVal() /*throws Exception*/ {
            string action = "$a.i = 1;";
            string expecting = "retval.i = 1;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a returns [int i, int j]: {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestIsolatedRefToCurrentRule() /*throws Exception*/ {
            string action = "$a;";
            //String expecting = "";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : 'a' {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates

            int expectedMsgID = ErrorManager.MSG_ISOLATED_RULE_SCOPE;
            object expectedArg = "a";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg,
                                            expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestIsolatedRefToRule() /*throws Exception*/ {
            string action = "$x;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : x=b {" + action + "}\n" +
                "  ;\n" +
                "b : 'b' ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates

            int expectedMsgID = ErrorManager.MSG_ISOLATED_RULE_SCOPE;
            object expectedArg = "x";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg );
            checkError( equeue, expectedMessage );
        }

        /*  I think these have to be errors $a.x makes no sense.
        [TestMethod] public void TestFullyQualifiedRefToLabelInCurrentRule()
            //throws Exception
        {
                String action = "$a.x;";
                String expecting = "x;";

                ErrorQueue equeue = new ErrorQueue();
                ErrorManager.setErrorListener(equeue);
                Grammar g = new Grammar(
                    "grammar t;\n"+
                        "a : x='a' {"+action+"}\n" +
                        "  ;\n");
                Tool antlr = newTool();
                CodeGenerator generator = new CodeGenerator(antlr, g, "Java");
                g.setCodeGenerator(generator);
                generator.genRecognizer(); // forces load of templates
                ActionTranslator translator = new ActionTranslator(generator,"a",
                                                                   new CommonToken(ANTLRParser.ACTION,action),1);
                String rawTranslation =
                    translator.translate();
                StringTemplateGroup templates =
                    new StringTemplateGroup(".", typeof(AngleBracketTemplateLexer));
                StringTemplate actionST = new StringTemplate(templates, rawTranslation);
                String found = actionST.ToString();
                assertEquals(expecting, found);

                assertEquals("unexpected errors: "+equeue, 0, equeue.errors.size());
            }

        [TestMethod] public void TestFullyQualifiedRefToListLabelInCurrentRule()
            //throws Exception
        {
            String action = "$a.x;"; // must be qualified
            String expecting = "list_x;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener(equeue);
            Grammar g = new Grammar(
                "grammar t;\n"+
                    "a : x+='a' {"+action+"}\n" +
                    "  ;\n");
            Tool antlr = newTool();
            CodeGenerator generator = new CodeGenerator(antlr, g, "Java");
            g.setCodeGenerator(generator);
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator(generator,"a",
                                                               new CommonToken(ANTLRParser.ACTION,action),1);
            String rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup(".", typeof(AngleBracketTemplateLexer));
            StringTemplate actionST = new StringTemplate(templates, rawTranslation);
            String found = actionST.ToString();
            assertEquals(expecting, found);

            assertEquals("unexpected errors: "+equeue, 0, equeue.errors.size());
        }
    */
        [TestMethod]
        public void TestFullyQualifiedRefToTemplateAttributeInCurrentRule() /*throws Exception*/ {
            string action = "$a.st;"; // can be qualified
            string expecting = "retval.st;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "options {output=template;}\n" +
                "a : (A->{$A.text}) {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRuleRefWhenRuleHasScope() /*throws Exception*/ {
            string action = "$b.start;";
            string expecting = "(b1!=null?((Token)b1.start):null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : b {###" + action + "!!!} ;\n" +
                "b\n" +
                "scope {\n" +
                "  int n;\n" +
                "} : 'b' \n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates

            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestDynamicScopeRefOkEvenThoughRuleRefExists() /*throws Exception*/ {
            string action = "$b::n;";
            string expecting = "((b_scope)b_stack.peek()).n;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "s : b ;\n" +
                "b\n" +
                "scope {\n" +
                "  int n;\n" +
                "} : '(' b ')' {" + action + "}\n" + // refers to current invocation's n
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRefToTemplateAttributeForCurrentRule() /*throws Exception*/ {
            string action = "$st=null;";
            string expecting = "retval.st =null;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "options {output=template;}\n" +
                "a : {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRefToTextAttributeForCurrentRule() /*throws Exception*/ {
            string action = "$text";
            string expecting = "input.toString(retval.start,input.LT(-1))";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "options {output=template;}\n" +
                "a : {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRefToStartAttributeForCurrentRule() /*throws Exception*/ {
            string action = "$start;";
            string expecting = "((Token)retval.start);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a : {###" + action + "!!!}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestTokenLabelFromMultipleAlts() /*throws Exception*/ {
            string action = "$ID.text;"; // must be qualified
            string action2 = "$INT.text;"; // must be qualified
            string expecting = "(ID1!=null?ID1.getText():null);";
            string expecting2 = "(INT2!=null?INT2.getText():null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ID {" + action + "}\n" +
                "  | INT {" + action2 + "}\n" +
                "  ;\n" +
                "ID : 'a';\n" +
                "INT : '0';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
            translator = new ActionTranslator( generator,
                                                   "a",
                                                   new CommonToken( ANTLRParser.ACTION, action2 ), 2 );
            rawTranslation =
                translator.translate();
            templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            actionST = new StringTemplate( templates, rawTranslation );
            found = actionST.ToString();

            assertEquals( expecting2, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRuleLabelFromMultipleAlts() /*throws Exception*/ {
            string action = "$b.text;"; // must be qualified
            string action2 = "$c.text;"; // must be qualified
            string expecting = "(b1!=null?input.toString(b1.start,b1.stop):null);";
            string expecting2 = "(c2!=null?input.toString(c2.start,c2.stop):null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : b {" + action + "}\n" +
                "  | c {" + action2 + "}\n" +
                "  ;\n" +
                "b : 'a';\n" +
                "c : '0';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
            translator = new ActionTranslator( generator,
                                                   "a",
                                                   new CommonToken( ANTLRParser.ACTION, action2 ), 2 );
            rawTranslation =
                translator.translate();
            templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            actionST = new StringTemplate( templates, rawTranslation );
            found = actionST.ToString();

            assertEquals( expecting2, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestUnknownDynamicAttribute() /*throws Exception*/ {
            string action = "$a::x";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a\n" +
                "scope {\n" +
                "  int n;\n" +
                "} : {" + action + "}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_UNKNOWN_DYNAMIC_SCOPE_ATTRIBUTE;
            object expectedArg = "a";
            object expectedArg2 = "x";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestUnknownGlobalDynamicAttribute() /*throws Exception*/ {
            string action = "$Symbols::x";
            string expecting = action;

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "scope Symbols {\n" +
                "  int n;\n" +
                "}\n" +
                "a : {'+action+'}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_UNKNOWN_DYNAMIC_SCOPE_ATTRIBUTE;
            object expectedArg = "Symbols";
            object expectedArg2 = "x";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestUnqualifiedRuleScopeAttribute() /*throws Exception*/ {
            string action = "$n;"; // must be qualified
            string expecting = "$n;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a\n" +
                "scope {\n" +
                "  int n;\n" +
                "} : b\n" +
                "  ;\n" +
                "b : {'+action+'}\n" +
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "b",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_UNKNOWN_SIMPLE_ATTRIBUTE;
            object expectedArg = "n";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestRuleAndTokenLabelTypeMismatch() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : id='foo' id=b\n" +
                "  ;\n" +
                "b : ;\n" );
            int expectedMsgID = ErrorManager.MSG_LABEL_TYPE_CONFLICT;
            object expectedArg = "id";
            object expectedArg2 = "rule!=token";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestListAndTokenLabelTypeMismatch() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ids+='a' ids='b'\n" +
                "  ;\n" +
                "b : ;\n" );
            int expectedMsgID = ErrorManager.MSG_LABEL_TYPE_CONFLICT;
            object expectedArg = "ids";
            object expectedArg2 = "token!=token-list";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestListAndRuleLabelTypeMismatch() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "options {output=AST;}\n" +
                "a : bs+=b bs=b\n" +
                "  ;\n" +
                "b : 'b';\n" );
            int expectedMsgID = ErrorManager.MSG_LABEL_TYPE_CONFLICT;
            object expectedArg = "bs";
            object expectedArg2 = "rule!=rule-list";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestArgReturnValueMismatch() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a[int i] returns [int x, int i]\n" +
                "  : \n" +
                "  ;\n" +
                "b : ;\n" );
            int expectedMsgID = ErrorManager.MSG_ARG_RETVAL_CONFLICT;
            object expectedArg = "i";
            object expectedArg2 = "a";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestSimplePlusEqualLabel() /*throws Exception*/ {
            string action = "$ids.size();"; // must be qualified
            string expecting = "list_ids.size();";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "parser grammar t;\n" +
                "a : ids+=ID ( COMMA ids+=ID {" + action + "})* ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestPlusEqualStringLabel() /*throws Exception*/ {
            string action = "$ids.size();"; // must be qualified
            string expecting = "list_ids.size();";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ids+='if' ( ',' ids+=ID {" + action + "})* ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestPlusEqualSetLabel() /*throws Exception*/ {
            string action = "$ids.size();"; // must be qualified
            string expecting = "list_ids.size();";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ids+=('a'|'b') ( ',' ids+=ID {" + action + "})* ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestPlusEqualWildcardLabel() /*throws Exception*/ {
            string action = "$ids.size();"; // must be qualified
            string expecting = "list_ids.size();";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ids+=. ( ',' ids+=ID {" + action + "})* ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestImplicitTokenLabel() /*throws Exception*/ {
            string action = "$ID; $ID.text; $ID.getText()";
            string expecting = "ID1; (ID1!=null?ID1.getText():null); ID1.getText()";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ID {" + action + "} ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );

            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestImplicitRuleLabel() /*throws Exception*/ {
            string action = "$r.start;";
            string expecting = "(r1!=null?((Token)r1.start):null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : r {###" + action + "!!!} ;" +
                "r : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestReuseExistingLabelWithImplicitRuleLabel() /*throws Exception*/ {
            string action = "$r.start;";
            string expecting = "(x!=null?((Token)x.start):null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : x=r {###" + action + "!!!} ;" +
                "r : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestReuseExistingListLabelWithImplicitRuleLabel() /*throws Exception*/ {
            string action = "$r.start;";
            string expecting = "(x!=null?((Token)x.start):null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "options {output=AST;}\n" +
                "a : x+=r {###" + action + "!!!} ;" +
                "r : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestReuseExistingLabelWithImplicitTokenLabel() /*throws Exception*/ {
            string action = "$ID.text;";
            string expecting = "(x!=null?x.getText():null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : x=ID {" + action + "} ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestReuseExistingListLabelWithImplicitTokenLabel() /*throws Exception*/ {
            string action = "$ID.text;";
            string expecting = "(x!=null?x.getText():null);";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : x+=ID {" + action + "} ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRuleLabelWithoutOutputOption() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar T;\n" +
                "s : x+=a ;" +
                "a : 'a';\n" +
                "b : 'b';\n" +
                "WS : ' '|'\n';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_LIST_LABEL_INVALID_UNLESS_RETVAL_STRUCT;
            object expectedArg = "x";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestRuleLabelOnTwoDifferentRulesAST() /*throws Exception*/ {
            Assert.Inconclusive( "I broke this test while trying to fix return values on another test..." );
            string grammar =
                "grammar T;\n" +
                "options {output=AST;}\n" +
                "s : x+=a x+=b {System.out.println($x);} ;" +
                "a : 'a';\n" +
                "b : 'b';\n" +
                "WS : (' '|'\\n') {skip();};\n";
            string expecting = "[a, b]" + NewLine + "a b" + NewLine;
            string found = execParser( "T.g", grammar, "TParser", "TLexer",
                                      "s", "a b", false );
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestRuleLabelOnTwoDifferentRulesTemplate() /*throws Exception*/ {
            Assert.Inconclusive( "I broke this test while trying to fix return values on another test..." );
            string grammar =
                "grammar T;\n" +
                "options {output=template;}\n" +
                "s : x+=a x+=b {System.out.println($x);} ;" +
                "a : 'a' -> {%{\"hi\"}} ;\n" +
                "b : 'b' -> {%{\"mom\"}} ;\n" +
                "WS : (' '|'\\n') {skip();};\n";
            string expecting = "[hi, mom]" + NewLine;
            string found = execParser( "T.g", grammar, "TParser", "TLexer",
                                      "s", "a b", false );
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestMissingArgs() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : r ;" +
                "r[int i] : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_MISSING_RULE_ARGS;
            object expectedArg = "r";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestArgsWhenNoneDefined() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : r[32,34] ;" +
                "r : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_RULE_HAS_NO_ARGS;
            object expectedArg = "r";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestReturnInitValue() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : r ;\n" +
                "r returns [int x=0] : 'a' {$x = 4;} ;\n" );
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );

            Rule r = g.getRule( "r" );
            AttributeScope retScope = r.returnScope;
            var parameters = retScope.Attributes;
            assertNotNull( "missing return action", parameters );
            assertEquals( 1, parameters.Count );
            string found = parameters.ElementAt( 0 ).ToString();
            string expecting = "int x=0";
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestMultipleReturnInitValue() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : r ;\n" +
                "r returns [int x=0, int y, String s=new String(\"foo\")] : 'a' {$x = 4;} ;\n" );
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );

            Rule r = g.getRule( "r" );
            AttributeScope retScope = r.returnScope;
            var parameters = retScope.Attributes;
            assertNotNull( "missing return action", parameters );
            assertEquals( 3, parameters.Count );
            assertEquals( "int x=0", parameters.ElementAt( 0 ).ToString() );
            assertEquals( "int y", parameters.ElementAt( 1 ).ToString() );
            assertEquals( "String s=new String(\"foo\")", parameters.ElementAt( 2 ).ToString() );
        }

        [TestMethod]
        public void TestCStyleReturnInitValue() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : r ;\n" +
                "r returns [int (*x)()=NULL] : 'a' ;\n" );
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );

            Rule r = g.getRule( "r" );
            AttributeScope retScope = r.returnScope;
            var parameters = retScope.Attributes;
            assertNotNull( "missing return action", parameters );
            assertEquals( 1, parameters.Count );
            string found = parameters.ElementAt( 0 ).ToString();
            string expecting = "int (*)() x=NULL";
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestArgsWithInitValues() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : r[32,34] ;" +
                "r[int x, int y=3] : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_ARG_INIT_VALUES_ILLEGAL;
            object expectedArg = "y";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestArgsOnToken() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ID[32,34] ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_ARGS_ON_TOKEN_REF;
            object expectedArg = "ID";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestArgsOnTokenInLexer() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : 'z' ID[32,34] ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_RULE_HAS_NO_ARGS;
            object expectedArg = "ID";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestLabelOnRuleRefInLexer() /*throws Exception*/ {
            string action = "$i.text";
            string expecting = "(i!=null?i.getText():null)";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : 'z' i=ID {" + action + "};" +
                "fragment ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "R",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();

            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRefToRuleRefInLexer() /*throws Exception*/ {
            string action = "$ID.text";
            string expecting = "(ID1!=null?ID1.getText():null)";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : 'z' ID {" + action + "};" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "R",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();

            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestRefToRuleRefInLexerNoAttribute() /*throws Exception*/ {
            string action = "$ID";
            string expecting = "ID1";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : 'z' ID {" + action + "};" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "R",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();

            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestCharLabelInLexer() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : x='z' ;\n" );

            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestCharListLabelInLexer() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : x+='z' ;\n" );

            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestWildcardCharLabelInLexer() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : x=. ;\n" );

            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestWildcardCharListLabelInLexer() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : x+=. ;\n" );

            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestMissingArgsInLexer() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "A : R ;" +
                "R[int i] : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_MISSING_RULE_ARGS;
            object expectedArg = "R";
            object expectedArg2 = null;
            // getting a second error @1:12, probably from nextToken
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestLexerRulePropertyRefs() /*throws Exception*/ {
            string action = "$text $type $line $pos $channel $index $start $stop";
            string expecting = "getText() _type state.tokenStartLine state.tokenStartCharPositionInLine _channel -1 state.tokenStartCharIndex (getCharIndex()-1)";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : 'r' {" + action + "};\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "R",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();

            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestLexerLabelRefs() /*throws Exception*/ {
            string action = "$a $b.text $c $d.text";
            string expecting = "a (b!=null?b.getText():null) c (d!=null?d.getText():null)";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : a='c' b='hi' c=. d=DUH {" + action + "};\n" +
                "DUH : 'd' ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "R",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();

            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestSettingLexerRulePropertyRefs() /*throws Exception*/ {
            string action = "$text $type=1 $line=1 $pos=1 $channel=1 $index";
            string expecting = "getText() _type=1 state.tokenStartLine=1 state.tokenStartCharPositionInLine=1 _channel=1 -1";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "lexer grammar t;\n" +
                "R : 'r' {" + action + "};\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "R",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();

            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestArgsOnTokenInLexerRuleOfCombined() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : R;\n" +
                "R : 'z' ID[32] ;\n" +
                "ID : 'a';\n" );

            string lexerGrammarStr = g.getLexerGrammar();
            System.IO.StringReader sr = new System.IO.StringReader( lexerGrammarStr );
            Grammar lexerGrammar = new Grammar();
            lexerGrammar.FileName = "<internally-generated-lexer>";
            lexerGrammar.importTokenVocabulary( g );
            lexerGrammar.parseAndBuildAST( sr );
            lexerGrammar.defineGrammarSymbols();
            lexerGrammar.checkNameSpaceAndActions();
            sr.Close();

            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, lexerGrammar, "Java" );
            lexerGrammar.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_RULE_HAS_NO_ARGS;
            object expectedArg = "ID";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, lexerGrammar, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestMissingArgsOnTokenInLexerRuleOfCombined() /*throws Exception*/ {
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : R;\n" +
                "R : 'z' ID ;\n" +
                "ID[int i] : 'a';\n" );

            string lexerGrammarStr = g.getLexerGrammar();
            StringReader sr = new StringReader( lexerGrammarStr );
            Grammar lexerGrammar = new Grammar();
            lexerGrammar.FileName = "<internally-generated-lexer>";
            lexerGrammar.importTokenVocabulary( g );
            lexerGrammar.parseAndBuildAST( sr );
            lexerGrammar.defineGrammarSymbols();
            lexerGrammar.checkNameSpaceAndActions();
            sr.Close();

            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, lexerGrammar, "Java" );
            lexerGrammar.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_MISSING_RULE_ARGS;
            object expectedArg = "ID";
            object expectedArg2 = null;
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, lexerGrammar, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        // T R E E S

        [TestMethod]
        public void TestTokenLabelTreeProperty() /*throws Exception*/ {
            string action = "$id.tree;";
            string expecting = "id_tree;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : id=ID {" + action + "} ;\n" +
                "ID : 'a';\n" );

            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            ActionTranslator translator =
                new ActionTranslator( generator,
                                          "a",
                                          new CommonToken( ANTLRParser.ACTION, action ), 1 );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestTokenRefTreeProperty() /*throws Exception*/ {
            string action = "$ID.tree;";
            string expecting = "ID1_tree;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ID {" + action + "} ;" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            ActionTranslator translator = new ActionTranslator( generator, "a",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestAmbiguousTokenRef() /*throws Exception*/ {
            string action = "$ID;";
            //String expecting = "";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ID ID {" + action + "};" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_NONUNIQUE_REF;
            object expectedArg = "ID";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestAmbiguousTokenRefWithProp() /*throws Exception*/ {
            string action = "$ID.text;";
            //String expecting = "";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "a : ID ID {" + action + "};" +
                "ID : 'a';\n" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();

            int expectedMsgID = ErrorManager.MSG_NONUNIQUE_REF;
            object expectedArg = "ID";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestRuleRefWithDynamicScope() /*throws Exception*/ {
            string action = "$field::x = $field.st;";
            string expecting = "((field_scope)field_stack.peek()).x = retval.st;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar a;\n" +
                "field\n" +
                "scope { StringTemplate x; }\n" +
                "    :   'y' {" + action + "}\n" +
                "    ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "field",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestAssignToOwnRulenameAttr() /*throws Exception*/ {
            string action = "$rule.tree = null;";
            string expecting = "retval.tree = null;";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar a;\n" +
                "rule\n" +
                "    : 'y' {" + action + "}\n" +
                "    ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "rule",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestAssignToOwnParamAttr() /*throws Exception*/ {
            string action = "$rule.i = 42; $i = 23;";
            string expecting = "i = 42; i = 23;";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar a;\n" +
                "rule[int i]\n" +
                "    : 'y' {" + action + "}\n" +
                "    ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "rule",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestIllegalAssignToOwnRulenameAttr() /*throws Exception*/ {
            string action = "$rule.stop = 0;";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar a;\n" +
                "rule\n" +
                "    : 'y' {" + action + "}\n" +
                "    ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "rule",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();

            int expectedMsgID = ErrorManager.MSG_WRITE_TO_READONLY_ATTR;
            object expectedArg = "rule";
            object expectedArg2 = "stop";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestIllegalAssignToLocalAttr() /*throws Exception*/ {
            string action = "$tree = null; $st = null; $start = 0; $stop = 0; $text = 0;";
            string expecting = "retval.tree = null; retval.st = null;   ";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar a;\n" +
                "rule\n" +
                "    : 'y' {" + action + "}\n" +
                "    ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "rule",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();

            int expectedMsgID = ErrorManager.MSG_WRITE_TO_READONLY_ATTR;
            var expectedErrors = new List<object>( 3 );
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, "start", "" );
            expectedErrors.Add( expectedMessage );
            GrammarSemanticsMessage expectedMessage2 =
                new GrammarSemanticsMessage( expectedMsgID, g, null, "stop", "" );
            expectedErrors.Add( expectedMessage2 );
            GrammarSemanticsMessage expectedMessage3 =
        new GrammarSemanticsMessage( expectedMsgID, g, null, "text", "" );
            expectedErrors.Add( expectedMessage3 );
            checkErrors( equeue, expectedErrors );

            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestIllegalAssignRuleRefAttr() /*throws Exception*/ {
            string action = "$other.tree = null;";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar a;\n" +
                "options { output = AST;}" +
                "otherrule\n" +
                "    : 'y' ;" +
                "rule\n" +
                "    : other=otherrule {" + action + "}\n" +
                "    ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "rule",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();

            int expectedMsgID = ErrorManager.MSG_WRITE_TO_READONLY_ATTR;
            object expectedArg = "other";
            object expectedArg2 = "tree";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestIllegalAssignTokenRefAttr() /*throws Exception*/ {
            string action = "$ID.text = \"test\";";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar a;\n" +
                "ID\n" +
                "    : 'y' ;" +
                "rule\n" +
                "    : ID {" + action + "}\n" +
                "    ;" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "rule",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();

            int expectedMsgID = ErrorManager.MSG_WRITE_TO_READONLY_ATTR;
            object expectedArg = "ID";
            object expectedArg2 = "text";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestAssignToTreeNodeAttribute() /*throws Exception*/ {
            string action = "$tree.scope = localScope;";
            string expecting = "(()retval.tree).scope = localScope;";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar a;\n" +
                "options { output=AST; }" +
                "rule\n" +
                "@init {\n" +
                "   Scope localScope=null;\n" +
                "}\n" +
                "@after {\n" +
                "   $tree.scope = localScope;\n" +
                "}\n" +
                "   : 'a' -> ^('a')\n" +
                ";" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "rule",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestDoNotTranslateAttributeCompare() /*throws Exception*/ {
            string action = "$a.line == $b.line";
            string expecting = "(a!=null?a.getLine():0) == (b!=null?b.getLine():0)";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                    "lexer grammar a;\n" +
                    "RULE:\n" +
                    "     a=ID b=ID {" + action + "}" +
                    "    ;\n" +
                    "ID : 'id';"
            );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "RULE",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestDoNotTranslateScopeAttributeCompare() /*throws Exception*/ {
            string action = "if ($rule::foo == \"foo\" || 1) { System.out.println(\"ouch\"); }";
            string expecting = "if (((rule_scope)rule_stack.peek()).foo == \"foo\" || 1) { System.out.println(\"ouch\"); }";
            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                    "grammar a;\n" +
                    "rule\n" +
                    "scope {\n" +
                    "   String foo;" +
                    "} :\n" +
                    "     twoIDs" +
                    "    ;\n" +
                    "twoIDs:\n" +
                    "    ID ID {" + action + "}\n" +
                    "    ;\n" +
                    "ID : 'id';"
            );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer();
            ActionTranslator translator = new ActionTranslator( generator,
                                                                         "twoIDs",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            // check that we didn't use scopeSetAttributeRef int translation!
            bool foundScopeSetAttributeRef = false;
            for ( int i = 0; i < translator.chunks.Count; i++ )
            {
                object chunk = translator.chunks[i];
                if ( chunk is StringTemplate )
                {
                    if ( ( (StringTemplate)chunk ).getName().Equals( "scopeSetAttributeRef" ) )
                    {
                        foundScopeSetAttributeRef = true;
                    }
                }
            }
            assertFalse( "action translator used scopeSetAttributeRef template in comparison!", foundScopeSetAttributeRef );
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
            assertEquals( expecting, found );
        }

        [TestMethod]
        public void TestTreeRuleStopAttributeIsInvalid() /*throws Exception*/ {
            string action = "$r.x; $r.start; $r.stop";
            string expecting = "(r!=null?r.x:0); (r!=null?((CommonTree)r.start):null); $r.stop";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "tree grammar t;\n" +
                "options {ASTLabelType=CommonTree;}\n" +
                "a returns [int x]\n" +
                "  :\n" +
                "  ;\n" +
                "b : r=a {###" + action + "!!!}\n" +
                "  ;" );
            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // codegen phase sets some vars we need
            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            int expectedMsgID = ErrorManager.MSG_UNKNOWN_RULE_ATTRIBUTE;
            object expectedArg = "a";
            object expectedArg2 = "stop";
            GrammarSemanticsMessage expectedMessage =
                new GrammarSemanticsMessage( expectedMsgID, g, null, expectedArg, expectedArg2 );
            Console.Out.WriteLine( "equeue:" + equeue );
            checkError( equeue, expectedMessage );
        }

        [TestMethod]
        public void TestRefToTextAttributeForCurrentTreeRule() /*throws Exception*/ {
            string action = "$text";
            string expecting = "input.getTokenStream().toString(" + NewLine +
                               "              input.getTreeAdaptor().getTokenStartIndex(retval.start)," + NewLine +
                               "              input.getTreeAdaptor().getTokenStopIndex(retval.start))";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "tree grammar t;\n" +
                "options {ASTLabelType=CommonTree;}\n" +
                "a : {###" + action + "!!!}\n" +
                "  ;\n" );

            AntlrTool antlr = newTool();
            antlr.setOutputDirectory( null ); // write to /dev/null
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // codegen phase sets some vars we need
            StringTemplate codeST = generator.RecognizerST;
            string code = codeST.ToString();
            string found = code.substring( code.IndexOf( "###" ) + 3, code.IndexOf( "!!!" ) );
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestTypeOfGuardedAttributeRefIsCorrect() /*throws Exception*/ {
            string action = "int x = $b::n;";
            string expecting = "int x = ((b_scope)b_stack.peek()).n;";

            ErrorQueue equeue = new ErrorQueue();
            ErrorManager.setErrorListener( equeue );
            Grammar g = new Grammar(
                "grammar t;\n" +
                "s : b ;\n" +
                "b\n" +
                "scope {\n" +
                "  int n;\n" +
                "} : '(' b ')' {" + action + "}\n" + // refers to current invocation's n
                "  ;\n" );
            AntlrTool antlr = newTool();
            CodeGenerator generator = new CodeGenerator( antlr, g, "Java" );
            g.setCodeGenerator( generator );
            generator.genRecognizer(); // forces load of templates
            ActionTranslator translator = new ActionTranslator( generator, "b",
                                                                         new CommonToken( ANTLRParser.ACTION, action ), 1 );
            string rawTranslation =
                translator.translate();
            StringTemplateGroup templates =
                new StringTemplateGroup( ".", typeof( AngleBracketTemplateLexer ) );
            StringTemplate actionST = new StringTemplate( templates, rawTranslation );
            string found = actionST.ToString();
            assertEquals( expecting, found );

            assertEquals( "unexpected errors: " + equeue, 0, equeue.errors.Count );
        }

        [TestMethod]
        public void TestGlobalAttributeScopeInit()
        {
            string grammar =
                "grammar foo;\n" +
                "scope S @scopeinit { this.value = true; } { boolean value; }\n" +
                "a scope S; : 'a' EOF {System.out.println($S::value);};\n";
            string found = execParser( "foo.g", grammar, "fooParser", "fooLexer", "a", "a", false );
            Assert.AreEqual( "true" + NewLine, found );
        }

        [TestMethod]
        public void TestRuleAttributeScopeInit()
        {
            string grammar =
                "grammar foo;\n" +
                "a scope @scopeinit { this.value = true; } { boolean value; } : 'a' EOF {System.out.println($a::value);};\n";
            string found = execParser( "foo.g", grammar, "fooParser", "fooLexer", "a", "a", false );
            Assert.AreEqual( "true" + NewLine, found );
        }

        // S U P P O R T

        protected void checkError( ErrorQueue equeue,
                                  GrammarSemanticsMessage expectedMessage )
        //throws Exception
        {
            /*
            System.out.println(equeue.infos);
            System.out.println(equeue.warnings);
            System.out.println(equeue.errors);
            */
            Message foundMsg = null;
            for ( int i = 0; i < equeue.errors.Count; i++ )
            {
                Message m = (Message)equeue.errors[i];
                if ( m.msgID == expectedMessage.msgID )
                {
                    foundMsg = m;
                }
            }
            assertTrue( "no error; " + expectedMessage.msgID + " expected", equeue.errors.Count > 0 );
            assertNotNull( "couldn't find expected error: " + expectedMessage.msgID + " in " + equeue, foundMsg );
            assertTrue( "error is not a GrammarSemanticsMessage",
                       foundMsg is GrammarSemanticsMessage );
            assertEquals( expectedMessage.arg, foundMsg.arg );
            assertEquals( expectedMessage.arg2, foundMsg.arg2 );
        }

        /** Allow checking for multiple errors in one test */
        protected void checkErrors( ErrorQueue equeue,
                                   List<object> expectedMessages )
        //throws Exception
        {
            var messageExpected = new List<object>( equeue.errors.Count );
            for ( int i = 0; i < equeue.errors.Count; i++ )
            {
                Message m = (Message)equeue.errors[i];
                bool foundMsg = false;
                for ( int j = 0; j < expectedMessages.Count; j++ )
                {
                    Message em = (Message)expectedMessages[j];
                    if ( m.msgID == em.msgID && m.arg.Equals( em.arg ) && m.arg2.Equals( em.arg2 ) )
                    {
                        foundMsg = true;
                    }
                }
                if ( foundMsg )
                {
                    messageExpected.Insert( i, true );
                }
                else
                    messageExpected.Insert( i, false );
            }
            for ( int i = 0; i < equeue.errors.Count; i++ )
            {
                assertTrue( "unexpected error:" + equeue.errors[i], ( (Boolean)messageExpected[i] ) );
            }
        }
    }
}
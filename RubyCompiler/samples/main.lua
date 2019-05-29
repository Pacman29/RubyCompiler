-- Necessary functionalities
require("nn")
require('optim')

-- Local requires

require("model")
require("train")
require("test")
require("data")


require('lfs')

-- Configurations

-- Prepare random number generator
math.randomseed(os.time())
torch.manualSeed(os.time())

-- Create namespaces
main = {}

-- The main program
function main.main()

   main.argparse()

   main.clock = {}
   main.clock.log = 0

   main.new()
   main.run()
 
end



-- Parse arguments
function main.argparse()
   local cmd = torch.CmdLine()

   -- Options
   cmd:option("-resume",0,"Resumption point in epoch. 0 means not resumption.")
   cmd:option("-debug",0,"debug. 0 means not debug.")
   cmd:option("-device",0,"device. 0 means cpu.")
   cmd:option("-format","","stk or py")
   cmd:option("-model","","lstm or cnn")
   cmd:text()

   -- Parse the option
   opt = cmd:parse(arg or {})

   if opt.debug > 0 then
      dbg = require("debugger")
   end

   if opt.format == "stk" and opt.model == "cnn" then
      print("Run stroke format and cnn model...")
      require("config_stroke_cnn")   
   elseif opt.format == "stk" and opt.model == "lstm" then
      print("Run stroke format and lstm model...")
      require("config_stroke_lstm")
   elseif opt.format == "py" and opt.model == "lstm" then
      print("Run pinyin format and lstm model...")
      require("config_pinyin_lstm")
   elseif opt.format == "py" and opt.model == "cnn" then
      print("Run pinyin format and cnn model...")
      require("config_pinyin_cnn")
   elseif opt.format == "wb2d" and opt.model == "cnn" then
      print("Run wubi format and cnn model...")
      require("config_wb_cnn_2d")
   elseif opt.format == "wb3d" and opt.model == "cnn" then
      print("Run wubi format and cnn model...")
      require("config_wb_cnn_3d")
   else 
      error("Wrong format")
   end

   -- Setting the device
   if opt.device > 0 then
      require("cutorch")
      require("cunn")
      cutorch.setDevice(opt.device)
      print("Device set to ".. opt.device)
      config.main.type = "torch.CudaTensor"
   else
      config.main.type = "torch.DoubleTensor"
   end

   -- Resumption operation
   if opt.resume > 0 then
      -- Find the main resumption file
      local files = main.findFiles(paths.concat(config.main.save,"main_"..tostring(opt.resume).."_"..
       opt.format .. "_" .. opt.model .. "_" ..
         config.train_data.length .. "_" ..config.dictsize.."_"..config.optim_name.."_*.t7b"))
      if #files ~= 1 then
    error("Found "..tostring(#files).." main resumption point.")
      end
      config.main.resume = files[1]
      print("Using main resumption point "..config.main.resume)
      -- Find the model resumption file
      local files = main.findFiles(paths.concat(config.main.save,"sequential_"..tostring(opt.resume).."_"..
         opt.format .. "_" .. opt.model .. "_" ..
         config.train_data.length .. "_" ..config.dictsize.."_"..config.optim_name.."_*.t7b"))
      if #files ~= 1 then
    error("Found "..tostring(#files).." model resumption point.")
      end
      config.model.file = files[1]
      print("Using model resumption point "..config.model.file)
      -- Resume the training epoch
      config.train.epoch = tonumber(opt.resume) + 1
      print("Next training epoch resumed to "..config.train.epoch)
      -- Don't do randomize
      if config.main.randomize then
         config.main.randomize = nil
         print("Disabled randomization for resumption")
      end
   end
end

-- Train a new experiment
function main.new()
   -- Load the data
   print("Loading datasets...")
   main.train_data = Data(config.train_data)
   main.val_data = Data(config.val_data)
   
   -- Load the model
   print("Loading the model...")
   main.model = Model(config.model)
   if config.main.randomize then
      main.model:randomize(config.main.randomize)
      print("Model randomized.")
   end
   main.model:type(config.main.type)
   print("Current model type: "..main.model:type())
   collectgarbage()

   -- Initiate the trainer
   print("Loading the trainer...")
   main.train = Train(main.train_data, main.model, config.loss(), config.train)

   -- Initiate the tester
   print("Loading the tester...")
   main.test_val = Test(main.val_data, main.model, config.loss(), config.test)

   -- The record structure
   main.record = {}

   collectgarbage()
end

-- Start the training
function main.run()
   --Run for this number of era
   for i = 1,config.main.eras do

      if config.main.dropout then
        print("Enabling dropouts")
        main.model:enableDropouts()
      else
        print("Disabling dropouts")
        main.model:disableDropouts()
      end
      print("Training for era "..i)
      if opt.format == "wb3d" then
         main.train:run_wb_3d(config.main.epoches, main.trainlog)
      elseif opt.format == "wb2d" then
         main.train:run_wb_2d(config.main.epoches, main.trainlog)
      else
         main.train:run(config.main.epoches, main.trainlog)
      end

      if config.main.validate == true then
        print("Disabling dropouts")
        main.model:disableDropouts()
        print("Testing on develop data for era "..i)
         if opt.format == "wb2d" then
            main.test_val:run_wb_2d(main.testlog)
         elseif opt.format == "wb3d" then
            main.test_val:run_wb_3d(main.testlog)
         else
            main.test_val:run(main.testlog)
         end
      end

      print("Recording on ear " .. i)
      main.record[#main.record + 1] = {val_error = main.test_val.e, val_loss = main.test_val.l}
      print("Visualizing loss")
      main.show()
      main.save()
      collectgarbage()
   end
end

function main.show()

   local epoch = torch.linspace(1, #main.record, #main.record):mul(config.main.epoches)
   local val_error = torch.zeros(#main.record)
   local val_loss = torch.zeros(#main.record)
   for i = 1, #main.record do
      val_error[i] = main.record[i].val_error
      val_loss[i] = main.record[i].val_loss
   end

   print("val_error is")
   print(val_error)

   print("val_loss is")
   print(val_loss)

end

function main.save()
   -- Record necessary configurations
   config.train.epoch = main.train.epoch

   if lfs.attributes(config.main.save) == nil then
         lfs.mkdir(config.main.save)
   end

   -- Make the save
   local time = os.time()
   torch.save(paths.concat(config.main.save,"main_"..(main.train.epoch-1).."_".. opt.format .. "_" .. opt.model .. "_" ..
      config.train_data.length .."_"..config.dictsize.."_"..config.optim_name.."_"..time..".t7b"),
         {config = config, record = main.record, momentum = main.train.old_grads:double()})
   torch.save(paths.concat(config.main.save,"sequential_"..(main.train.epoch-1).."_".. opt.format .. "_" .. opt.model .. "_" ..
      config.train_data.length .."_"..config.dictsize.."_"..config.optim_name.."_"..time..".t7b"),
         main.model:clearSequential(main.model:makeCleanSequential(main.model.sequential)))

   collectgarbage()
end

-- The training logging function
function main.trainlog(train)
   if config.main.collectgarbage and math.fmod(train.epoch-1,config.main.collectgarbage) == 0 then
      print("Collecting garbage at epoch = "..(train.epoch-1))
      collectgarbage()
   end

   if (os.time() - main.clock.log) >= (config.main.logtime or 1) then
      local msg = ""

        msg = msg.."epo: "..(train.epoch-1)..
       ", rat: "..string.format("%.2e",train.rate)..
       ", err: "..string.format("%.2e",train.error)..
       ", obj: "..string.format("%.2e",train.objective)

      print(msg)
   
      main.clock.log = os.time()
   end
end

function main.testlog(test)
   if config.main.collectgarbage and math.fmod(test.n,config.train_data.batch_size*config.main.collectgarbage) == 0 then
      print("Collecting garbage at n = "..test.n)
      collectgarbage()
   end
   if (os.time() - main.clock.log) >= (config.main.logtime or 1) then
      
      print("n: "..test.n..
          ", e: "..string.format("%.2e",test.e)..
          ", l: "..string.format("%.2e",test.l)..
          ", err: "..string.format("%.2e",test.err)..
          ", obj: "..string.format("%.2e",test.objective))
      main.clock.log = os.time()
   end
end

-- Utility function: find files with the specific 'ls' pattern
function main.findFiles(pattern)
   require("sys")
   local cmd = "ls "..pattern
   local str = sys.execute(cmd)
   local files = {}
   for file in str:gmatch("[^\n]+") do
      files[#files+1] = file
   end
   return files
end

-- Execute the main program
main.main()
